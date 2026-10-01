# Screenshotuwu

Lightweight Windows tray app. It sits idle in the notification area, then freezes the desktop the moment you click the icon so you can drag a rectangle and leave that capture floating on screen.

## Use

1. Run `Screenshotuwu`.
2. Click the uwu tray icon in the notification area (bottom-right). Right-click the icon for **Capture** or **Exit**.
3. Drag a rectangle across any monitor. Escape or right-click cancels.
4. The capture stays as a borderless picture where you drew it.
5. Click and drag the picture to move it.
6. Right-click the picture:
   - **Copy** — puts PNG (and a bitmap fallback) on the clipboard, including any highlighter or ink.
   - **Save as PNG** / **Save as JPG** — writes the picture including markup.
   - **Highlight** — toggle a translucent marker. Repeat to turn it off.
   - **Highlight color** / **Highlight thickness** — pick the marker.
   - **Draw** — toggle a thin line. Repeat to turn it off.
   - **Draw color** / **Draw thickness** — pick the pen.
   Only one tool can be on at a time.
7. With both tools off, click the picture (without dragging) to dismiss it. Unsaved captures are discarded.

## Run

```bash
dotnet run -c Release
```

That `bin/Release/net8.0-windows/Screenshotuwu.exe` is only a stub. It will not start if you copy it alone (it needs the `.dll` and `.runtimeconfig.json` beside it).

For a Desktop shortcut, either:

- Right-click the project-folder exe → **Create shortcut**, then move the shortcut; or
- Publish a single file you *can* copy:

```bash
dotnet publish -c Release -p:PublishProfile=Desktop
```

Copy `bin/Release/net8.0-windows/win-x64/publish/Screenshotuwu.exe` anywhere. Still needs the .NET 8 desktop runtime installed.

Windows 10/11.
