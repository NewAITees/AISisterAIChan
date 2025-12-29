# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is "AI Sister Ai-chan" (AI妹アイちゃん), an SSP/Ukagaka ghost that integrates Ollama LLM for conversational AI. The ghost uses local LLM inference via Ollama's OpenAI-compatible API endpoint instead of ChatGPT's cloud API.

**Key Technology Stack:**
- **SHIORI Interface**: SHIOLINK.dll (proxy DLL that launches C# scripts)
- **Script Engine**: Rosalind.CSharp.exe (C# scripting runtime based on Roslyn)
- **Language**: C# Scripts (.csx files) - not compiled C# projects
- **LLM Integration**: Ollama via HTTP API at `http://localhost:11434/v1/chat/completions`
- **Character Encoding**: UTF-8 (specified in descript.txt and SHIOLINK.INI)

## Architecture

### SHIORI Protocol Flow

```
SSP.exe → SHIOLINK.dll → Rosalind.CSharp.exe → Ghost.csx (entry point)
                                                      ↓
                                        Loads all .csx scripts via #load directives
```

When SSP sends SHIORI events (boot, mouse clicks, timers), SHIOLINK.dll:
1. Launches Rosalind.CSharp.exe as a child process
2. Sends SHIORI requests via stdin in PIPE communication
3. Receives SHIORI responses via stdout
4. Returns responses to SSP

### Script Module Architecture

**Entry Point**: `Ghost.csx`
- Defines `AISisterAIChanGhost` partial class extending `Shiorose.Ghost`
- Loads other modules via `#load` directives (order matters!)

**Module Dependencies** (load order):
```
Ghost.csx
├─ SaveData.csx          // Data persistence (JSON serialization)
├─ ChatGPT.csx           // Ollama API integration (streaming responses)
├─ CollisionParts.csx    // Mouse collision area definitions
├─ GhostMenu.csx         // Menu system and settings UI
├─ Surfaces.csx          // Surface ID mapping to expressions
└─ Log.csx               // Debug logging utilities
```

**Key Classes:**
- `AISisterAIChanGhost` (partial across Ghost.csx and GhostMenu.csx): Main ghost logic
- `ChatGPTTalk` (ChatGPT.csx): Async HTTP streaming client for Ollama API
- `SaveData` (SaveData.csx): Persistent settings (API key, talk interval, profiles, choice count)
- `Surfaces` (Surfaces.csx): Maps emotion categories to surface IDs
- `SurfaceCategory` (SurfaceCategory.csx): Emotion constants (普通, 驚き, etc.)

### LLM Integration Pattern

**Ollama Configuration:**
- Model: `gpt-oss:20b` (configurable in Ghost.csx line 211)
- Endpoint: `http://localhost:11434/v1/chat/completions` (ChatGPT.csx line 31)
- Stream mode: Always enabled for real-time response display

**Conversation Flow:**
1. User interaction triggers `BeginTalk()` (Ghost.csx:157)
2. Constructs prompt with character profiles + conversation history
3. Creates `ChatGPTTalk` instance that fires async HTTP streaming request
4. `OnSecondChange()` polls `ChatGPTTalk.Response` and builds SSP script via `BuildTalk()`
5. Parses LLM response to extract dialogue, emotion, and choice options using regex
6. Formats as SSP script with surface changes (`\s[ID]`) and user choices

**Response Parsing** (Ghost.csx:367-420):
- Dialogue: Matches `アイのセリフ：{text}` or `アイ：{text}` patterns
- Emotion: Matches `アイの表情：{category}` to select surface ID
- User choices: Matches `兄のセリフ候補N：{text}` patterns (N=1,2,3)
- Brackets trimming: Removes 「」『』"" quotation marks from dialogue

### Surface System

**Surface IDs** are numeric identifiers for character sprites (PNG files in `shell/master/`).

**Naming Convention**: `XXX_YYY.png` where:
- `XXX` = base surface category (000=normal, 001=embarrassed, 002=surprise, etc.)
- `YYY` = variant/expression detail

**Emotion Mapping** (Surfaces.csx):
- Each `SurfaceCategory` maps to array of surface IDs
- `GetSurfaceFromRate(rate)`: Deterministic selection based on rate (0.0-1.0)
- `GetRaodomSurface()`: Random selection from category

**Surface Selection Logic**:
1. Random rate generated once per talk (Ghost.csx:162): `faceRate = random.NextDouble()`
2. LLM returns emotion category (e.g., "驚き")
3. `GetSurfaceId()` (Ghost.csx:407) finds matching category
4. Uses `faceRate` to select specific surface within category → deterministic during a conversation

### Mouse Interaction System

**Collision Parts** (CollisionParts.csx):
- Define clickable/hoverable areas: head, cheek, mouth, ribbon, twintail, bust, skirt, shoulder, leg
- Each part ID maps to Japanese display name

**Interaction Handlers** (Ghost.csx):
- `OnMouseDoubleClick`: Triggers talk with `{USERName}：（{AIName}の{parts}をつつく）`
- `OnMouseStroke`: Mouse move → `（{AIName}の{parts}を撫でる）`
- `OnMouseClick` (button 2): Middle click → `（{AIName}の{parts}をつまむ）`
- `OnMouseWheel`: Scroll up/down → special actions per body part
- `OnMouseMove`: Head hover → wait-for-pet animation (`\s[101]`)

### Save Data System

**File**: `ghost/master/savedata.json` (auto-created)

**SaveData Properties**:
- `APIKey`: Ollama doesn't need this, but kept for compatibility
- `TalkInterval` / `TalkInterval2`: Random talk frequency in seconds
- `ChoiceCount`: Number of dialogue choices to show (0-3)
- `AiProfile` / `UserProfile`: Dictionary<string, string> for custom character traits
- `IsDevMode`: Enables logging to `ghost/master/log/` (prompt.txt, response.txt)
- `IsRandomIdlingSurfaceEnabled`: Periodic idle animation

**Persistence**: Auto-saved via `BaseSaveData` serialization (JSON format)

## Common Development Tasks

### Testing the Ghost

**Prerequisites:**
1. Install Ollama: https://ollama.ai
2. Pull a model: `ollama pull gpt-oss:20b` (or change model name in Ghost.csx:211)
3. Verify Ollama is running: `curl http://localhost:11434/v1/models`

**Load in SSP:**
- Place this directory in SSP's `ghost/` folder
- Right-click SSP tray icon → Change Ghost → Select "AI妹アイちゃん"

**Trigger Interactions:**
- Double-click anywhere (not on body) → Opens menu
- Double-click body parts → Triggers conversation
- Wait for random talk (default: 5 minutes)
- Right-click → Communicate → Type custom message

### Debugging

**Enable Developer Mode:**
1. Double-click ghost (anywhere except body)
2. Select "設定を変えたい" (Change settings)
3. Toggle "開発者モードを変更する" to enable

**Debug Outputs** (when DevMode enabled):
- `ghost/master/log/prompt.txt`: Full prompt sent to Ollama
- `ghost/master/log/response.txt`: Raw LLM response
- `ghost/master/SHIOLINK.log`: SHIORI protocol communication log

**Common Issues:**
- Garbled text → Check encoding in SHIOLINK.INI (should be UTF-8)
- No response → Verify Ollama is running and model is pulled
- Wrong dialogue format → Check prompt template in Ghost.csx:165-203
- Surface not changing → Verify LLM outputs emotion in format `アイの表情：{category}`

### Modifying the Character

**Change Character Names** (Ghost.csx:22-23):
```csharp
const string AIName = "アイ";      // Ghost's name
const string USERName = "兄";      // User's name
```

**Edit Character Profile** (Ghost.csx:169-186):
- Modify prompt template strings for personality, appearance, relationships
- Or use in-game menu: Menu → "プロフィールを変更する"

**Change LLM Model** (Ghost.csx:211):
```csharp
model = "gpt-oss:20b",  // Change to any Ollama model
```

**Adjust Random Talk Frequency** (SaveData.csx:50):
```csharp
TalkInterval = 300;  // Default: 300 seconds (5 minutes)
```

### Adding New Emotions

1. Add constant to `SurfaceCategory.csx`:
```csharp
public const string NewEmotion = "新しい感情";
```

2. Map surface IDs in `Surfaces.csx`:
```csharp
[SurfaceCategory.NewEmotion] = new Surfaces(1001, 1002, 1003),
```

3. Add to emotion list in prompt (Ghost.csx:194):
```csharp
{SurfaceCategory.All.Select(x=>$"「{x}」").Aggregate((a,b)=>a+b)}
```

4. Create corresponding PNG files in `shell/master/`: `1001.png`, `1002.png`, etc.

### Extending the Script System

**Adding a New Module:**
1. Create `NewModule.csx` in `ghost/master/`
2. Add `#load "NewModule.csx"` to `Ghost.csx` (before usage)
3. Use `#r "Rosalind.dll"` if you need Shiorose types

**Extending AISisterAIChanGhost:**
- Use `partial class AISisterAIChanGhost` to split logic across files
- Override more `Ghost` base class methods (see Rosalind.xml documentation)

## Important Constants and Character Names

**Hardcoded Identifiers** (used in parsing):
- `AIName` / `USERName`: Used in prompt and response regex patterns
- When changing names, update regex patterns in `GetAIResponse()` and `GetOnichanRenponse()`

**SSP Script Format Markers:**
- `\s[N]`: Switch to surface N
- `\0` / `\1`: Switch to character 0 (sakura) / 1 (kero)
- `\e`: End script
- `\_q`: Quick/no-wait mode
- `\n`: Line break
- Choice marker requires `Marker().AppendChoice()` via TalkBuilder

## File Encoding

All text files in this project use **UTF-8 encoding**:
- `ghost/master/descript.txt`: `charset,UTF8`
- `ghost/master/SHIOLINK.INI`: `charmode = UTF-8`
- All .csx scripts should be saved as UTF-8

When editing files, ensure your editor maintains UTF-8 encoding to prevent mojibake (文字化け).
