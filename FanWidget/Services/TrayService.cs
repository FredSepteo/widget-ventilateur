using System.Drawing;
using System.IO;
using System.Windows.Forms;
using DrawingIcon = System.Drawing.Icon;

namespace FanWidget.Services;

public sealed class TrayService : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly Action _onShow;
    private readonly Action _onExit;
    private bool _disposed;

    public TrayService(Action onShow, Action onExit)
    {
        _onShow = onShow;
        _onExit = onExit;

        var menu = new ContextMenuStrip();
        menu.Items.Add("Afficher", null, (_, _) => _onShow());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Quitter", null, (_, _) => _onExit());

        _notifyIcon = new NotifyIcon
        {
            Text = "Contrôle ventilateurs",
            Icon = LoadIcon(),
            Visible = true,
            ContextMenuStrip = menu,
        };

        _notifyIcon.DoubleClick += (_, _) => _onShow();
    }

    public void ShowBalloon(string title, string text)
    {
        _notifyIcon.BalloonTipTitle = title;
        _notifyIcon.BalloonTipText = text;
        _notifyIcon.ShowBalloonTip(2500);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }

    private static DrawingIcon LoadIcon()
    {
        var icoPath = Path.Combine(AppContext.BaseDirectory, "fan.ico");
        if (File.Exists(icoPath))
            return new DrawingIcon(icoPath);

        return CreateFallbackIcon();
    }

    private static DrawingIcon CreateFallbackIcon()
    {
        using var bitmap = new Bitmap(32, 32);
        using var g = Graphics.FromImage(bitmap);
        g.Clear(Color.FromArgb(15, 20, 25));
        using var brush = new SolidBrush(Color.FromArgb(0, 200, 160));
        g.FillEllipse(brush, 4, 4, 24, 24);
        using var hub = new SolidBrush(Color.FromArgb(20, 28, 36));
        g.FillEllipse(hub, 12, 12, 8, 8);
        return DrawingIcon.FromHandle(bitmap.GetHicon());
    }
}
