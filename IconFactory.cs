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
        using var bmp = new Bitmap(32, 32, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            using var fill = new SolidBrush(Color.FromArgb(255, 255, 143, 191));
            g.FillEllipse(fill, 1, 1, 30, 30);

            using var ring = new Pen(Color.FromArgb(255, 255, 255, 255), 2f);
            g.DrawEllipse(ring, 6, 8, 20, 16);

            using var lens = new SolidBrush(Color.FromArgb(255, 72, 36, 64));
            g.FillEllipse(lens, 12, 11, 8, 8);

            using var shine = new SolidBrush(Color.FromArgb(180, 255, 255, 255));
            g.FillEllipse(shine, 14, 13, 3, 3);
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
