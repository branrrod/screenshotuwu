using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace Screenshotuwu;

internal sealed class FloatingShot : Form
{
    private enum ToolMode
    {
        None,
        Highlight,
        Draw
    }

    // Opacity of one highlighter pass. A stroke never darkens over itself; a new stroke stacks.
    private const float HighlightOpacity = 0.35f;

    private static readonly (string Name, Color Value)[] HighlightPalette =
    {
        ("Yellow", Color.FromArgb(255, 255, 224, 32)),
        ("Pink", Color.FromArgb(255, 255, 143, 191)),
        ("Green", Color.FromArgb(255, 140, 230, 90)),
        ("Blue", Color.FromArgb(255, 90, 190, 255)),
        ("Orange", Color.FromArgb(255, 255, 160, 50)),
    };

    private static readonly (string Name, Color Value)[] DrawPalette =
    {
        ("Red", Color.FromArgb(255, 226, 59, 74)),
        ("Black", Color.FromArgb(255, 40, 40, 40)),
        ("White", Color.FromArgb(255, 250, 250, 250)),
        ("Blue", Color.FromArgb(255, 40, 110, 220)),
        ("Pink", Color.FromArgb(255, 255, 105, 180)),
    };

    private static readonly (string Name, float Value)[] HighlightThicknesses =
    {
        ("Thin", 10f),
        ("Medium", 18f),
        ("Thick", 28f),
        ("Extra thick", 40f),
    };

    private static readonly (string Name, float Value)[] DrawThicknesses =
    {
        ("1 px", 1f),
        ("2 px", 2.4f),
        ("4 px", 4f),
        ("6 px", 6f),
        ("10 px", 10f),
    };

    private readonly Bitmap _photo;
    private readonly Bitmap _markup;
    private readonly ToolStripMenuItem _highlightItem;
    private readonly ToolStripMenuItem _drawItem;
    private readonly ToolStripMenuItem _highlightColorMenu;
    private readonly ToolStripMenuItem _highlightThicknessMenu;
    private readonly ToolStripMenuItem _drawColorMenu;
    private readonly ToolStripMenuItem _drawThicknessMenu;

    private ToolMode _mode = ToolMode.None;
    private Color _highlightRgb = HighlightPalette[0].Value;
    private Color _drawColor = DrawPalette[0].Value;
    private float _highlightWidth = HighlightThicknesses[1].Value;
    private float _drawWidth = DrawThicknesses[1].Value;
    private bool _stroking;
    private bool _moving;
    private bool _moved;
    private Point _lastPoint;
    private Point _grabOffset;
    private HighlightStroke? _highlightStroke;

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

        _highlightColorMenu = BuildColorMenu("Highlight color", HighlightPalette, c => _highlightRgb = c);
        _highlightThicknessMenu = BuildThicknessMenu("Highlight thickness", HighlightThicknesses, w => _highlightWidth = w);
        _drawColorMenu = BuildColorMenu("Draw color", DrawPalette, c => _drawColor = c);
        _drawThicknessMenu = BuildThicknessMenu("Draw thickness", DrawThicknesses, w => _drawWidth = w);

        var menu = new ContextMenuStrip();
        menu.Items.Add("Copy", null, (_, _) => CopyToClipboard());
        menu.Items.Add("Save as PNG", null, (_, _) => Save(ImageFormat.Png, "png", "PNG image|*.png"));
        menu.Items.Add("Save as JPG", null, (_, _) => Save(ImageFormat.Jpeg, "jpg", "JPEG image|*.jpg"));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_highlightItem);
        menu.Items.Add(_highlightColorMenu);
        menu.Items.Add(_highlightThicknessMenu);
        menu.Items.Add(_drawItem);
        menu.Items.Add(_drawColorMenu);
        menu.Items.Add(_drawThicknessMenu);
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
            EndHighlightStroke();
            if (_mode == ToolMode.Highlight)
                _highlightStroke = new HighlightStroke(_markup, _highlightRgb, _highlightWidth, HighlightOpacity);
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
                EndHighlightStroke();
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
        EndHighlightStroke();
        _moving = false;
        Cursor = _mode == ToolMode.None ? Cursors.SizeAll : Cursors.Cross;
        SyncMenuChecks();
    }

    private void SyncMenuChecks()
    {
        _highlightItem.Checked = _mode == ToolMode.Highlight;
        _drawItem.Checked = _mode == ToolMode.Draw;
        SyncChoiceChecks(_highlightColorMenu, item => ((Color)item.Tag!).ToArgb() == _highlightRgb.ToArgb());
        SyncChoiceChecks(_drawColorMenu, item => ((Color)item.Tag!).ToArgb() == _drawColor.ToArgb());
        SyncChoiceChecks(_highlightThicknessMenu, item => (float)item.Tag! == _highlightWidth);
        SyncChoiceChecks(_drawThicknessMenu, item => (float)item.Tag! == _drawWidth);
    }

    private static void SyncChoiceChecks(ToolStripMenuItem menu, Func<ToolStripMenuItem, bool> isSelected)
    {
        foreach (ToolStripItem raw in menu.DropDownItems)
        {
            if (raw is ToolStripMenuItem item)
                item.Checked = isSelected(item);
        }
    }

    private static ToolStripMenuItem BuildColorMenu(
        string title,
        (string Name, Color Value)[] palette,
        Action<Color> setter)
    {
        var menu = new ToolStripMenuItem(title);
        foreach ((string name, Color value) in palette)
        {
            var item = new ToolStripMenuItem(name) { Tag = value };
            item.Click += (_, _) => setter(value);
            menu.DropDownItems.Add(item);
        }

        return menu;
    }

    private static ToolStripMenuItem BuildThicknessMenu(
        string title,
        (string Name, float Value)[] options,
        Action<float> setter)
    {
        var menu = new ToolStripMenuItem(title);
        foreach ((string name, float value) in options)
        {
            var item = new ToolStripMenuItem(name) { Tag = value };
            item.Click += (_, _) => setter(value);
            menu.DropDownItems.Add(item);
        }

        return menu;
    }

    private void EndHighlightStroke()
    {
        _highlightStroke?.Dispose();
        _highlightStroke = null;
    }

    private void Stroke(Point from, Point to)
    {
        if (_highlightStroke is not null)
        {
            Rectangle dirty = _highlightStroke.AddSegment(from, to);
            if (!dirty.IsEmpty)
                Invalidate(dirty);
            return;
        }

        using var g = Graphics.FromImage(_markup);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.CompositingMode = CompositingMode.SourceOver;

        float width = _drawWidth;
        using var pen = new Pen(_drawColor, width)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };
        g.DrawLine(pen, from, to);

        int pad = (int)Math.Ceiling(width / 2f) + 2;
        int x = Math.Max(0, Math.Min(from.X, to.X) - pad);
        int y = Math.Max(0, Math.Min(from.Y, to.Y) - pad);
        int w = Math.Min(_markup.Width - x, Math.Abs(from.X - to.X) + pad * 2);
        int h = Math.Min(_markup.Height - y, Math.Abs(from.Y - to.Y) + pad * 2);
        if (w > 0 && h > 0)
            Invalidate(new Rectangle(x, y, w, h));
    }

    private void CopyToClipboard()
    {
        try
        {
            using Bitmap composite = Composite();
            using var png = new MemoryStream();
            composite.Save(png, ImageFormat.Png);
            byte[] bytes = png.ToArray();

            var data = new DataObject();
            data.SetImage(composite);
            data.SetData("PNG", false, new MemoryStream(bytes));
            Clipboard.SetDataObject(data, copy: true);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                $"Could not copy the image:\n{ex.Message}",
                "Screenshotuwu",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
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
        EndHighlightStroke();
        _photo.Dispose();
        _markup.Dispose();
        base.OnFormClosed(e);
    }
}
