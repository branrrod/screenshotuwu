using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Screenshotuwu;

/// <summary>
/// One highlighter stroke. The stroke is drawn fully opaque on its own layer, so the
/// segments of a single stroke never build up where they overlap, and that layer is
/// composited onto the markup at one uniform opacity. A new stroke over the same spot
/// composites on top of the earlier one, so separate passes stack like a real marker.
/// </summary>
internal sealed class HighlightStroke : IDisposable
{
    private readonly Bitmap _target;
    private readonly Bitmap _base;
    private readonly Bitmap _layer;
    private readonly Pen _pen;
    private readonly ImageAttributes _fade;
    private readonly float _width;

    public HighlightStroke(Bitmap target, Color rgb, float width, float opacity)
    {
        _target = target;
        _base = target.Clone(new Rectangle(Point.Empty, target.Size), PixelFormat.Format32bppArgb);
        _layer = new Bitmap(target.Width, target.Height, PixelFormat.Format32bppArgb);
        _width = width;
        _pen = new Pen(Color.FromArgb(255, rgb), width)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };
        _fade = new ImageAttributes();
        _fade.SetColorMatrix(new ColorMatrix { Matrix33 = opacity }, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
    }

    /// <summary>Adds a segment and refreshes the markup. Returns the area that changed.</summary>
    public Rectangle AddSegment(Point from, Point to)
    {
        using (var g = Graphics.FromImage(_layer))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.CompositingMode = CompositingMode.SourceOver;
            g.DrawLine(_pen, from, to);
        }

        int pad = (int)Math.Ceiling(_width / 2f) + 2;
        Rectangle dirty = Rectangle.FromLTRB(
            Math.Min(from.X, to.X) - pad,
            Math.Min(from.Y, to.Y) - pad,
            Math.Max(from.X, to.X) + pad + 1,
            Math.Max(from.Y, to.Y) + pad + 1);
        dirty.Intersect(new Rectangle(Point.Empty, _target.Size));
        if (dirty.IsEmpty)
            return dirty;

        // Markup = what was there before this stroke + this stroke's layer at uniform opacity.
        using (var g = Graphics.FromImage(_target))
        {
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.CompositingMode = CompositingMode.SourceCopy;
            g.DrawImage(_base, dirty, dirty, GraphicsUnit.Pixel);
            g.CompositingMode = CompositingMode.SourceOver;
            g.DrawImage(_layer, dirty, dirty.X, dirty.Y, dirty.Width, dirty.Height, GraphicsUnit.Pixel, _fade);
        }

        return dirty;
    }

    public void Dispose()
    {
        _base.Dispose();
        _layer.Dispose();
        _pen.Dispose();
        _fade.Dispose();
    }
}