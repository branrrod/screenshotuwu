using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Screenshotuwu;

internal sealed class FloatingShot : Form
{
    private enum ToolMode
    {
        None,
        Highlight,
        Draw
    }

    private static readonly Color HighlightColor = Color.FromArgb(90, 255, 224, 32);
    private static readonly Color DrawColor = Color.FromArgb(255, 226, 59, 74);
    private const float HighlightWidth = 18f;
    private const float DrawWidth = 2.4f;

    private readonly Bitmap _photo;
    private readonly Bitmap _markup;
    private readonly ToolStripMenuItem _highlightItem;
    private readonly ToolStripMenuItem _drawItem;

    private ToolMode _mode = ToolMode.None;
    private bool _stroking;
    private bool _moving;
    private bool _moved;
    private Point _lastPoint;
    private Point _grabOffset;

    public FloatingShot(Bitmap photo, Point screenLocation)
    {
        _photo = photo;
        _markup = new Bitmap(photo.Width, photo.Height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(_markup))
            g.Clear(Color.Transparent);

        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        Location = screenLocation;
        ClientSize = photo.Size;
        Cursor = Cursors.SizeAll;
        KeyPreview = true;
        SetStyle(
            ControlStyles.AllPaintingInWmPaint
            | ControlStyles.UserPaint
            | ControlStyles.OptimizedDoubleBuffer,
            true);

        _highlightItem = new ToolStripMenuItem("Highlight");
        _highlightItem.Click += (_, _) => ToggleTool(ToolMode.Highlight);
        _drawItem = new ToolStripMenuItem("Draw");
        _drawItem.Click += (_, _) => ToggleTool(ToolMode.Draw);

        var menu = new ContextMenuStrip();
        menu.Items.Add("Save as PNG", null, (_, _) => Save(ImageFormat.Png, "png", "PNG image|*.png"));
        menu.Items.Add("Save as JPG", null, (_, _) => Save(ImageFormat.Jpeg, "jpg", "JPEG image|*.jpg"));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_highlightItem);
        menu.Items.Add(_drawItem);
        menu.Opening += (_, _) => SyncMenuChecks();
        ContextMenuStrip = menu;
    }

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW — hide from Alt+Tab
            return cp;
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.CompositingMode = CompositingMode.SourceCopy;
        g.DrawImageUnscaled(_photo, 0, 0);
        g.CompositingMode = CompositingMode.SourceOver;
        g.DrawImageUnscaled(_markup, 0, 0);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
        {
            base.OnMouseDown(e);
            return;
        }

        if (_mode == ToolMode.None)
        {
            _moving = true;
            _moved = false;
            _grabOffset = e.Location;
            Capture = true;
        }
        else
        {
            _stroking = true;
            _lastPoint = e.Location;
            Capture = true;
            Stroke(_lastPoint, _lastPoint);
        }

        base.OnMouseDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_stroking)
        {
            Stroke(_lastPoint, e.Location);
            _lastPoint = e.Location;
        }
        else if (_moving)
        {
            if (!_moved && Geometry.ExceedsDragThreshold(_grabOffset, e.Location))
                _moved = true;

            if (_moved)
            {
                Point screen = PointToScreen(e.Location);
                Location = new Point(screen.X - _grabOffset.X, screen.Y - _grabOffset.Y);
            }
        }

        base.OnMouseMove(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            Capture = false;
            if (_stroking)
            {
                _stroking = false;
            }
            else if (_moving)
            {
                _moving = false;
                if (!_moved && _mode == ToolMode.None)
                    Close();
            }
        }

        base.OnMouseUp(e);
    }

    private void ToggleTool(ToolMode mode)
    {
        _mode = _mode == mode ? ToolMode.None : mode;
        _stroking = false;
        _moving = false;
        Cursor = _mode == ToolMode.None ? Cursors.SizeAll : Cursors.Cross;
        SyncMenuChecks();
    }

    private void SyncMenuChecks()
    {
        _highlightItem.Checked = _mode == ToolMode.Highlight;
        _drawItem.Checked = _mode == ToolMode.Draw;
    }

    private void Stroke(Point from, Point to)
    {
        using var g = Graphics.FromImage(_markup);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.CompositingMode = CompositingMode.SourceOver;

        bool highlight = _mode == ToolMode.Highlight;
        using var pen = new Pen(
            highlight ? HighlightColor : DrawColor,
            highlight ? HighlightWidth : DrawWidth)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };
        g.DrawLine(pen, from, to);

        int pad = (int)Math.Ceiling((highlight ? HighlightWidth : DrawWidth) / 2f) + 2;
        int x = Math.Max(0, Math.Min(from.X, to.X) - pad);
        int y = Math.Max(0, Math.Min(from.Y, to.Y) - pad);
        int w = Math.Min(_markup.Width - x, Math.Abs(from.X - to.X) + pad * 2);
        int h = Math.Min(_markup.Height - y, Math.Abs(from.Y - to.Y) + pad * 2);
        if (w > 0 && h > 0)
            Invalidate(new Rectangle(x, y, w, h));
    }

    private void Save(ImageFormat format, string extension, string filter)
    {
        using var dialog = new SaveFileDialog
        {
            Title = "Save screenshot",
            Filter = filter,
            DefaultExt = extension,
            AddExtension = true,
            OverwritePrompt = true,
            FileName = $"screenshotuwu_{DateTime.Now:yyyyMMdd_HHmmss}.{extension}"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            using Bitmap composite = Composite();
            if (format.Equals(ImageFormat.Jpeg))
                SaveJpeg(composite, dialog.FileName);
            else
                composite.Save(dialog.FileName, format);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                $"Could not save the image:\n{ex.Message}",
                "Screenshotuwu",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private Bitmap Composite()
    {
        var result = new Bitmap(_photo.Width, _photo.Height, PixelFormat.Format24bppRgb);
        using var g = Graphics.FromImage(result);
        g.CompositingMode = CompositingMode.SourceCopy;
        g.DrawImageUnscaled(_photo, 0, 0);
        g.CompositingMode = CompositingMode.SourceOver;
        g.DrawImageUnscaled(_markup, 0, 0);
        return result;
    }

    private static void SaveJpeg(Bitmap bitmap, string path)
    {
        ImageCodecInfo? codec = ImageCodecInfo.GetImageEncoders()
            .FirstOrDefault(c => c.FormatID == ImageFormat.Jpeg.Guid);
        if (codec is null)
        {
            bitmap.Save(path, ImageFormat.Jpeg);
            return;
        }

        using var parameters = new EncoderParameters(1);
        parameters.Param[0] = new EncoderParameter(Encoder.Quality, 92L);
        bitmap.Save(path, codec, parameters);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _photo.Dispose();
        _markup.Dispose();
        base.OnFormClosed(e);
    }
}
