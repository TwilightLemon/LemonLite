using System.Reflection;

namespace LemonLite.Utils;

public sealed class ScrollViewer : FluentWpfCore.Controls.SmoothScrollViewer
{
    public void AnimatedScrollToVerticalOffset(double offset)
    {
        //HandleScroll(double deltaVertical, double deltaHorizontal, bool isPreciseMode=false)
        var methodInfo = typeof(FluentWpfCore.Controls.SmoothScrollViewer)
            .GetMethod("HandleScroll", BindingFlags.NonPublic | BindingFlags.Instance);
        methodInfo?.Invoke(this, [VerticalOffset - offset, 0.0, false]);
    }
}