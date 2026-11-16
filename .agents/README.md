# AI agent configuration

This folder holds tool-agnostic AI agent config so anyone can develop this project
with any AI coding agent (OpenCode, Claude Code, Codex, Amp, ...).

## Skills

All 21 skills live in [`skills/`](./skills) as `<skill-id>/SKILL.md` folders
([Agent Skills](https://agentskills.io) open standard, MIT, from
[microsoft/winappCli](https://github.com/microsoft/winappCli/tree/main/plugins)).

They cover the WinApp CLI (`winapp`) workflows and WinUI 3 development this project uses:
setup, build/run, XAML design, code review, UI testing, MSIX packaging + signing,
API/control lookup, performance, sandbox, and troubleshooting.

| Tool        | Discovery                                                                    |
| ----------- | ---------------------------------------------------------------------------- |
| OpenCode    | reads `.agents/skills` natively (project compatibility source)               |
| Amp, others | read `.agents/skills` natively (Agent Skills convention)                     |
| Claude Code | reads `.claude/skills` only — link it once (see below), it follows symlinks  |

### Claude Code (one-time, per clone)

Windows (no admin needed):

```powershell
New-Item -ItemType Junction -Path .claude\skills -Target .agents\skills
```

macOS / Linux:

```sh
mkdir -p .claude && ln -s ../.agents/skills .claude/skills
```

(`.claude/` is gitignored, so each developer creates their own link.)

### Adding or updating skills

Edit files directly under `.agents/skills/` — every tool picks them up from there.
Do not duplicate skill folders into tool-specific directories; keep one canonical copy.
