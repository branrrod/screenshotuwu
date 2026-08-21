# Screenshotuwu

Lightweight Windows tray app. It sits idle in the notification area, then freezes the desktop the moment you click the icon so you can drag a rectangle and leave that capture floating on screen.

## Use

1. Run `Screenshotuwu`.
2. Click the pink camera icon in the notification area (bottom-right). Right-click the icon for **Capture** or **Exit**.
3. Drag a rectangle across any monitor. Escape or right-click cancels.
4. The capture stays as a borderless picture where you drew it.
5. Click and drag the picture to move it.
6. Right-click the picture:
   - **Save as PNG** / **Save as JPG** — writes the picture including any highlighter or ink.
   - **Highlight** — toggle a thick yellow marker. Repeat to turn it off.
   - **Draw** — toggle a thin red line. Repeat to turn it off.
   Only one tool can be on at a time.
7. With both tools off, click the picture (without dragging) to dismiss it. Unsaved captures are discarded.

## Run

```bash
dotnet run -c Release
```

Build output: `bin/Release/net8.0-windows/Screenshotuwu.exe`

Windows 10/11. Requires the .NET 8 desktop runtime (or SDK) to run from source.
