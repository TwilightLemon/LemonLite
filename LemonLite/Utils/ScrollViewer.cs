using System.Windows.Controls;

namespace LemonLite.Utils;

public sealed class ScrollViewer : FluentWpfCore.Controls.SmoothScrollViewer
{
    public ScrollViewer()
    {
        this.Physics = new FluentWpfCore.ScrollPhysics.DefaultScrollPhysics()
        {
            MinVelocityFactor=1.5,
            Friction=0.95
        };
        VirtualizingPanel.SetCacheLength(this, new VirtualizationCacheLength(2.0, 2.0));
    }
}