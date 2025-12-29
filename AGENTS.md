# Repository Guidelines

## Project Structure & Module Organization
- `ghost/master/` contains the core C# script modules (`*.csx`), DLLs, and runtime config like `SHIOLINK.INI`, `descript.txt`, and `savedata.json` (auto-created).
- `ghost/master/Ghost.csx` is the entry point; it `#load`s the other modules in order.
- `shell/master/` holds character art and surface assets (`*.png`) plus `surfaces.txt` and `descript.txt` for the shell.
- Root docs like `README.md` and `CLAUDE.md` explain usage, architecture, and troubleshooting.

## Build, Test, and Development Commands
There is no build step; the ghost is executed by SSP with the C# scripting runtime.
- Install a model: `ollama pull gpt-oss:20b` (or your preferred model).
- Verify Ollama is running: `curl http://localhost:11434/v1/models`.
- Run locally: place this repo in your SSP `ghost/` folder, then select the ghost in SSP.
- Change the model: edit `ghost/master/Ghost.csx` (see `model = "..."`).

## Coding Style & Naming Conventions
- C# scripts are `*.csx` with 4-space indentation and UTF-8 encoding.
- Keep `#load` order stable in `ghost/master/Ghost.csx` since module dependencies rely on it.
- Surface assets follow `XXX_YYY.png` naming where `XXX` is the base category and `YYY` is a variant (see `shell/master/`).
- Keep partial class extensions (`AISisterAIChanGhost`) in `ghost/master/` modules.

## Testing Guidelines
- No automated test suite exists; testing is manual in SSP.
- For debugging, enable developer mode in the ghost menu and inspect logs in `ghost/master/log/` (prompt/response and SHIOLINK logs).

## Commit & Pull Request Guidelines
- Commit messages are free-form (often short, descriptive, and sometimes in Japanese). Keep them concise and specific to the change.
- PRs should include a short summary, testing notes (e.g., SSP manual verification), and screenshots or GIFs for shell/UI changes. Link related issues if applicable.

## Configuration & Runtime Notes
- Ollama uses the local OpenAI-compatible endpoint at `http://localhost:11434/v1/chat/completions`.
- Encoding is UTF-8; keep `ghost/master/SHIOLINK.INI` and `ghost/master/descript.txt` consistent to avoid mojibake.
