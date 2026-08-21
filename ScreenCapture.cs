namespace Screenshotuwu;

internal static class ScreenCapture
{
    public static Bitmap CaptureVirtualScreen()
    {
        Rectangle vs = SystemInformation.VirtualScreen;
        var bitmap = new Bitmap(vs.Width, vs.Height);
        using var g = Graphics.FromImage(bitmap);
        g.CopyFromScreen(vs.Left, vs.Top, 0, 0, vs.Size);
        return bitmap;
    }
}
