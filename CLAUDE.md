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
├─ Log.csx               // Debug logging utilities
└─ KeroCharacter.csx     // Kero (companion character) settings and surface mappings
```

**Key Classes:**
- `AISisterAIChanGhost` (partial across Ghost.csx and GhostMenu.csx): Main ghost logic
- `ChatGPTTalk` (ChatGPT.csx): Async HTTP streaming client for Ollama API
- `SaveData` (SaveData.csx): Persistent settings (API key, talk interval, profiles, choice count)
- `Surfaces` (Surfaces.csx): Maps emotion categories to surface IDs
- `SurfaceCategory` (SurfaceCategory.csx): Emotion constants (普通, 驚き, etc.)
- `KeroSettings` (KeroCharacter.csx): Companion character (Kero) personality and profile settings
- `KeroSurfaceCategory` (KeroCharacter.csx): Kero-specific emotion categories
- `KeroSurfaces` (KeroCharacter.csx): Surface mapping for Kero character

### LLM Integration Pattern

**Ollama Configuration:**
- Model: `gpt-oss:20b` (configurable in Ghost.csx, search for `model =`)
- Endpoint: `http://localhost:11434/v1/chat/completions` (ChatGPT.csx)
- Stream mode: Always enabled for real-time response display

**Conversation Flow:**
1. User interaction triggers `BeginTalk()` or `BeginManzai()` or `BeginSoloTalk()`
2. Constructs prompt with character profiles + conversation history (mode-dependent)
3. Creates `ChatGPTTalk` instance that fires async HTTP streaming request
4. `OnSecondChange()` polls `ChatGPTTalk.Response` and builds SSP script via `BuildTalk()` or `BuildManzaiTalk()`
5. Parses LLM response to extract dialogue, emotion, and choice options using regex
6. Formats as SSP script with surface changes (`\s[ID]`) and user choices

**Response Parsing** (Ghost.csx):
- Dialogue: Matches `アイのセリフ：{text}` or `アイ：{text}` patterns
- Emotion: Matches `アイの表情：{category}` to select surface ID
- User choices: Matches `兄のセリフ候補N：{text}` patterns (N=1,2,3)
- Brackets trimming: Removes 「」『』"" quotation marks from dialogue
- Manzai mode: Matches numbered patterns `{Name}のセリフN：{text}` and `{Name}の表情N：{category}`

### Talk Modes

The ghost supports three different conversation modes via `TalkMode` enum (Ghost.csx:25-30):

**1. Normal Mode (TalkMode.Normal)**
- Standard one-on-one conversation between user and Ai-chan
- User interactions trigger `BeginTalk()` which generates prompts for Ai's responses
- LLM outputs: dialogue, emotion, conversation continuation flag, user choice suggestions
- Can be triggered by: mouse interactions, random talk, communicate menu

**2. Solo Talk Mode (TalkMode.Solo)**
- Ai-chan talks to herself (monologue)
- Triggered via menu: "独り言をする"
- Uses `BuildSoloPrompt()` (Ghost.csx:184-217) with different prompt template
- No user interaction choices displayed - just displays Ai's thoughts
- Always ends immediately (会話継続：「終了」)
- Useful for character personality demonstration

**3. Manzai Mode (TalkMode.Manzai)**
- Comedy duo conversation between Ai (ボケ担当/funny role) and Kero/Teddy (ツッコミ担当/straight man)
- Triggered via menu: "漫才モード"
- Uses `BuildManzaiPrompt()` (Ghost.csx:222-296) for duo conversation generation
- Generates 3-5 back-and-forth exchanges with punchline
- LLM outputs numbered dialogue pairs: `{Name}のセリフN`, `{Name}の表情N` (N=1,2,3...)
- Parsing via `GetAllManzaiResponses()` (Ghost.csx:930-995)
- Script building via `BuildManzaiTalk()` (Ghost.csx:740-925)
- Characters alternate: `\0` (Ai) and `\1` (Kero)
- User can continue manzai, talk to either character individually, or end

**Kero Character (Companion)**
- Name: テディ (Teddy) - defined in KeroSettings.Name
- Personality: けだるいダウナー系 (lethargic/downtempo type)
- Role: ツッコミ担当 (straight man in comedy duo)
- Appearance: Same visual design as Ai (pink hair, twin tails)
- Uses same surface IDs as Ai but with different emotion mapping via KeroSurfaceCategory
- Key traits: Tired, unmotivated, gives curt responses like "はあ..." "まあ..."

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
1. Random rate generated once per talk: `faceRate = random.NextDouble()`
2. LLM returns emotion category (e.g., "驚き")
3. `GetSurfaceId()` finds matching category
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
- Double-click anywhere (not on body) → Opens menu with options:
  - 独り言をする (Solo Talk) - Ai-chan monologue
  - なにか話して (Random Talk) - Normal conversation
  - 話しかける (Communicate) - Type custom message
  - 漫才モード (Manzai Mode) - Comedy duo conversation with Kero
  - プロフィールを変更する (Change Profile)
  - 設定を変えたい (Settings)
- Double-click body parts → Triggers normal conversation
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
- Wrong dialogue format → Check prompt templates in `BuildSoloPrompt()`, `BuildManzaiPrompt()`, or normal mode prompt
- Surface not changing → Verify LLM outputs emotion in format `アイの表情：{category}` (or `{Name}の表情N：{category}` for manzai)
- Manzai dialogue not parsing → Check that LLM outputs numbered patterns like `アイのセリフ1：`, `テディのセリフ1：`, etc.

### Modifying the Character

**Change Character Names** (Ghost.csx):
```csharp
const string AIName = "アイ";      // Ghost's name
const string USERName = "兄";      // User's name
```
Also update `KeroSettings.Name` in KeroCharacter.csx for companion character name.

**Edit Character Profile**:
- Modify prompt template strings in `BuildSoloPrompt()`, `BuildManzaiPrompt()`, or normal mode prompt for personality, appearance, relationships
- Or use in-game menu: Menu → "プロフィールを変更する"

**Change LLM Model** (Ghost.csx):
```csharp
model = "gpt-oss:20b",  // Change to any Ollama model (appears in multiple BeginTalk methods)
```

**Adjust Random Talk Frequency** (SaveData.csx):
```csharp
TalkInterval = 300;  // Default: 300 seconds (5 minutes)
```

**Customize Kero Character** (KeroCharacter.csx):
- Edit `KeroSettings.Name` to change companion name
- Modify `KeroSettings.Personality` for character traits
- Update `KeroSettings.Profile` dictionary for custom attributes

### Adding New Emotions

1. Add constant to `SurfaceCategory.csx`:
```csharp
public const string NewEmotion = "新しい感情";
```

2. Map surface IDs in `Surfaces.csx`:
```csharp
[SurfaceCategory.NewEmotion] = new Surfaces(1001, 1002, 1003),
```

3. Add to `SurfaceCategory.All` array in `SurfaceCategory.csx`

4. Emotion will automatically appear in prompts via:
```csharp
{SurfaceCategory.All.Select(x=>$"「{x}」").Aggregate((a,b)=>a+b)}
```

5. Create corresponding PNG files in `shell/master/`: `1001.png`, `1002.png`, etc.

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
- `KeroSettings.Name`: Kero character name (default: "テディ")
- When changing names, update:
  - Regex patterns in `GetAIResponse()` and `GetOnichanRenponse()`
  - Manzai parsing patterns in `GetAllManzaiResponses()`
  - `descript.txt` entries for `sakura.name` and `kero.name`

**SSP Script Format Markers:**
- `\s[N]`: Switch to surface N
- `\0` / `\1`: Switch to character 0 (sakura/Ai) / 1 (kero/Teddy)
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
