# PomodoroTray

A lightweight Pomodoro timer that lives in the Windows taskbar tray.
The remaining minutes are drawn directly on the tray icon as crisp pixel
digits — no tiny system font, no opening windows to check the time.

## Features

- **Pixel-clock tray icon** — remaining minutes rendered edge-to-edge as
  hand-designed pixel digits. The color tells you the phase at a glance:
  - 🔴 red — work session
  - 🟢 green — short break
  - 🔵 blue — long break
  - ⚪ grey — paused
- **Control popup** (left-click the tray icon) — four icon buttons with
  tooltips: Start/Pause, Reset, Skip phase, and Pin. While pinned, the
  popup stays on top of other windows until you unpin it.
- **Right-click menu** — the same controls as text items, plus settings:
  - **Autostart** (on by default) — the next phase begins immediately
    when the current one ends; the first phase also starts at app launch.
  - **Notifications** (on by default) — balloon tip when a phase ends.
- **Settings persist** between launches in
  `%AppData%\PomodoroTray\settings.json`.
- **Default phases**: 25 min work / 5 min short break / 15 min long break
  (long break after every 4th session).

## Download

Grab the exe from the [Releases](../../releases) page. Two builds are
published:

| File | .NET required? | Size | Use when |
|------|----------------|------|----------|
| `PomodoroTray-self-contained.exe` | No | ~150 MB | You just want it to run — copy and double-click |
| `PomodoroTray-framework-dependent.exe` | Yes | ~0.2 MB | You already have (or want) a small file and don't mind installing the runtime |

If you pick the framework-dependent build, install the
[.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)
(version **8.x**, the *Desktop Runtime* for Windows x64) once, then run
the exe.

## Run from source

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```powershell
dotnet run -c Release
```

## Tests

```powershell
dotnet test
```

## Build distributable exes

```powershell
.\publish.ps1
```

Produces both variants in `dist/`:

- `dist/PomodoroTray-self-contained.exe`
- `dist/PomodoroTray-framework-dependent.exe`

## Start with Windows

Press `Win+R`, type `shell:startup`, Enter — then drop a shortcut to the
exe into the folder that opens.

## Configuration

- **Phase durations** — edit `PomodoroEngine.cs`
  (`WorkMinutes`, `ShortBreakMinutes`, `LongBreakMinutes`,
  `SessionsBeforeLongBreak`) and rebuild.
- **Autostart / Notifications** — toggle from the tray icon's right-click
  menu; saved automatically.
- **Icon colors per phase** — `PixelClock.GetColor`.

## Project structure

- `Program.cs` — entry point
- `PomodoroEngine.cs` — pure timer/phase logic (no UI)
- `PixelClock.cs` — pixel digit glyphs + icon rendering
- `IconRenderer.cs` — adapts the rendered square to the native tray size
- `PopupPanel.cs` — control popup next to the tray
- `TrayApplicationContext.cs` — wires NotifyIcon, menu, settings, cache
- `AppSettings.cs` — JSON settings in `%AppData%\PomodoroTray`
- `PomodoroTray.Tests/` — xunit tests for glyphs, layout, icon size

## License

[MIT](LICENSE)
