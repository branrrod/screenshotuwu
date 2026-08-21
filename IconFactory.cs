using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Screenshotuwu;

internal static class IconFactory
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    public static Icon Create()
    {
        try
        {
            Icon? associated = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            if (associated is not null)
                return associated;
        }
        catch
        {
            // Fall through to the sidecar .ico or the drawn fallback.
        }

        foreach (string candidate in IcoCandidates())
        {
            if (!File.Exists(candidate))
                continue;

            try
            {
                return new Icon(candidate);
            }
            catch
            {
                // Try the next path.
            }
        }

        return CreateFallback();
    }

    private static IEnumerable<string> IcoCandidates()
    {
        string baseDir = AppContext.BaseDirectory;
        yield return Path.Combine(baseDir, "Assets", "screenshotuwu.ico");
        yield return Path.Combine(baseDir, "screenshotuwu.ico");
    }

    private static Icon CreateFallback()
    {
        using var bmp = new Bitmap(32, 32, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            using var fill = new SolidBrush(Color.FromArgb(255, 248, 244, 240));
            g.FillEllipse(fill, 1, 1, 30, 30);
            using var ink = new Pen(Color.FromArgb(255, 32, 32, 32), 2.2f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };
            g.DrawArc(ink, 7, 9, 7, 8, 200, 140);
            g.DrawArc(ink, 18, 9, 7, 8, 200, 140);
            g.DrawArc(ink, 13, 16, 6, 5, 20, 140);
            using var blush = new Pen(Color.FromArgb(255, 255, 158, 176), 1.6f);
            g.DrawLine(blush, 6, 20, 8, 18);
            g.DrawLine(blush, 8, 21, 10, 19);
            g.DrawLine(blush, 24, 18, 26, 20);
            g.DrawLine(blush, 22, 19, 24, 21);
        }

        IntPtr handle = bmp.GetHicon();
        try
        {
            using var temp = Icon.FromHandle(handle);
            return (Icon)temp.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }
}
