namespace Screenshotuwu;

internal sealed class TrayApp : ApplicationContext
{
    private readonly NotifyIcon _tray;
    private SelectionOverlay? _overlay;
    private bool _disposed;

    public TrayApp()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Capture", null, (_, _) => BeginCapture());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(CreateStartWithWindowsItem());
        menu.Items.Add("Exit", null, (_, _) => Quit());

        _tray = new NotifyIcon
        {
            Icon = IconFactory.Create(),
            Text = "Screenshotuwu",
            Visible = true,
            ContextMenuStrip = menu
        };
        _tray.MouseClick += OnTrayClick;
    }

    private static ToolStripMenuItem CreateStartWithWindowsItem()
    {
        bool enabled;
        try
        {
            enabled = Autostart.Sync();
        }
        catch
        {
            enabled = false; // registry unavailable: show unchecked, never block startup
        }

        var item = new ToolStripMenuItem("Start with Windows") { CheckOnClick = true, Checked = enabled };
        item.CheckedChanged += (_, _) =>
        {
            try
            {
                Autostart.SetEnabled(item.Checked);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not update Start with Windows: {ex.Message}", "Screenshotuwu",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };
        return item;
    }

    private void OnTrayClick(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
            BeginCapture();
    }

    private void BeginCapture()
    {
        if (_overlay is { IsDisposed: false, Visible: true })
            return;

        Bitmap snapshot;
        try
        {
            snapshot = ScreenCapture.CaptureVirtualScreen();
        }
        catch (Exception ex)
        {
            _tray.ShowBalloonTip(
                2500,
                "Screenshotuwu",
                $"Could not capture the screen: {ex.Message}",
                ToolTipIcon.Error);
            return;
        }

        _overlay = new SelectionOverlay(snapshot);
        _overlay.SelectionCompleted += OnSelectionCompleted;
        _overlay.Cancelled += OnOverlayCancelled;
        _overlay.Show();
        _overlay.Activate();
    }

    private void OnSelectionCompleted(Rectangle screenRect, Bitmap crop)
    {
        // Overlay closes itself after this event; only detach so we do not double-dispose.
        DetachOverlay(dispose: false);
        var shot = new FloatingShot(crop, screenRect.Location);
        shot.Show();
    }

    private void OnOverlayCancelled()
    {
        DetachOverlay(dispose: false);
    }

    private void DetachOverlay(bool dispose)
    {
        if (_overlay is null)
            return;

        SelectionOverlay overlay = _overlay;
        _overlay = null;
        overlay.SelectionCompleted -= OnSelectionCompleted;
        overlay.Cancelled -= OnOverlayCancelled;
        if (dispose && !overlay.IsDisposed)
            overlay.Dispose();
    }

    private void Quit() => ExitThread();

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            _disposed = true;
            DetachOverlay(dispose: true);
            Icon? icon = _tray.Icon;
            _tray.Icon = null;
            _tray.Visible = false;
            _tray.Dispose();
            icon?.Dispose();
        }

        base.Dispose(disposing);
    }
}
