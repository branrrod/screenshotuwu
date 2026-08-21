namespace Screenshotuwu;

internal static class Geometry
{
    public const int DragThresholdPx = 5;
    public const int MinCapturePx = 4;

    public static Rectangle Normalize(Point a, Point b)
    {
        int x = Math.Min(a.X, b.X);
        int y = Math.Min(a.Y, b.Y);
        return new Rectangle(x, y, Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
    }

    public static bool ExceedsDragThreshold(Point a, Point b, int threshold = DragThresholdPx)
        => Math.Abs(a.X - b.X) > threshold || Math.Abs(a.Y - b.Y) > threshold;

    public static Rectangle ClampTo(Rectangle rect, Size bounds)
    {
        int x = Math.Clamp(rect.X, 0, Math.Max(0, bounds.Width - 1));
        int y = Math.Clamp(rect.Y, 0, Math.Max(0, bounds.Height - 1));
        int right = Math.Clamp(rect.Right, x + 1, bounds.Width);
        int bottom = Math.Clamp(rect.Bottom, y + 1, bounds.Height);
        return Rectangle.FromLTRB(x, y, right, bottom);
    }
}
