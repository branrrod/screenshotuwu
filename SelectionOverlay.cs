using System.Drawing.Drawing2D;

namespace Screenshotuwu;

internal sealed class SelectionOverlay : Form
{
    private readonly Bitmap _snapshot;
    private readonly Bitmap _dimmed;
    private Point _origin;
    private Rectangle _rect;
    private bool _dragging;
    private bool _finished;

    public event Action<Rectangle, Bitmap>? SelectionCompleted;
    public event Action? Cancelled;

    public SelectionOverlay(Bitmap snapshot)
    {
        _snapshot = snapshot;
        _dimmed = BuildDimmed(snapshot);

        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        Cursor = Cursors.Cross;
        KeyPreview = true;
        Bounds = SystemInformation.VirtualScreen;
        SetStyle(
            ControlStyles.AllPaintingInWmPaint
            | ControlStyles.UserPaint
            | ControlStyles.OptimizedDoubleBuffer,
            true);
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

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        Activate();
        BringToFront();
        TopMost = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
            FinishCancel();
        base.OnKeyDown(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (_finished)
            return;

        if (e.Button == MouseButtons.Left)
        {
            _dragging = true;
            Capture = true;
            _origin = e.Location;
            _rect = new Rectangle(_origin, Size.Empty);
            Invalidate();
        }
        else if (e.Button == MouseButtons.Right)
        {
            FinishCancel();
        }

        base.OnMouseDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_dragging && !_finished)
        {
            _rect = Geometry.Normalize(_origin, e.Location);
            Invalidate();
        }

        base.OnMouseMove(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (!_dragging || e.Button != MouseButtons.Left || _finished)
        {
            base.OnMouseUp(e);
            return;
        }

        _dragging = false;
        Capture = false;
        Rectangle cropRect = Geometry.ClampTo(_rect, _snapshot.Size);
        if (cropRect.Width < Geometry.MinCapturePx || cropRect.Height < Geometry.MinCapturePx)
        {
            FinishCancel();
            base.OnMouseUp(e);
            return;
        }

        Bitmap crop;
        try
        {
            crop = _snapshot.Clone(cropRect, _snapshot.PixelFormat);
        }
        catch (Exception)
        {
            FinishCancel();
            base.OnMouseUp(e);
            return;
        }

        var screenRect = new Rectangle(
            Left + cropRect.X,
            Top + cropRect.Y,
            cropRect.Width,
            cropRect.Height);

        FinishSuccess(screenRect, crop);
        base.OnMouseUp(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
        g.DrawImageUnscaled(_dimmed, 0, 0);

        if (_rect.Width > 0 && _rect.Height > 0)
        {
            g.SetClip(_rect);
            g.DrawImageUnscaled(_snapshot, 0, 0);
            g.ResetClip();
            g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
            g.SmoothingMode = SmoothingMode.None;
            using var pen = new Pen(Color.FromArgb(255, 255, 180, 220), 1f);
            g.DrawRectangle(pen, _rect.X, _rect.Y, Math.Max(0, _rect.Width - 1), Math.Max(0, _rect.Height - 1));
        }
    }

    private void FinishSuccess(Rectangle screenRect, Bitmap crop)
    {
        if (_finished)
        {
            crop.Dispose();
            return;
        }

        _finished = true;
        Hide();
        SelectionCompleted?.Invoke(screenRect, crop);
        Close();
    }

    private void FinishCancel()
    {
        if (_finished)
            return;

        _finished = true;
        _dragging = false;
        Capture = false;
        Hide();
        Cancelled?.Invoke();
        Close();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _dimmed.Dispose();
        _snapshot.Dispose();
        base.OnFormClosed(e);
    }

    private static Bitmap BuildDimmed(Bitmap snapshot)
    {
        var dimmed = new Bitmap(snapshot.Width, snapshot.Height, snapshot.PixelFormat);
        using var g = Graphics.FromImage(dimmed);
        g.DrawImageUnscaled(snapshot, 0, 0);
        using var veil = new SolidBrush(Color.FromArgb(120, 0, 0, 0));
        g.FillRectangle(veil, 0, 0, dimmed.Width, dimmed.Height);
        return dimmed;
    }
}
