using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace LemonLite.Utils;

public class WindowResizeAdorner : Adorner
{
    readonly Thumb _leftThumb, _topThumb, _rightThumb, _bottomThumb;
    readonly Thumb _lefTopThumb, _rightTopThumb, _rightBottomThumb, _leftbottomThumb;
    readonly Grid _grid;
    readonly UIElement _adornedElement;
    readonly Window _window;

    /// <summary>
    /// false = 完全禁用所有拖拽调整
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Island 模式：只允许左右拖宽，最小宽度 60px，高度和四角全部锁死
    /// </summary>
    public bool IslandMode { get; set; } = false;

    private const double IslandMinWidth = 60d;

    public WindowResizeAdorner(UIElement adornedElement) : base(adornedElement)
    {
        _adornedElement = adornedElement;
        _window = Window.GetWindow(_adornedElement);

        _leftThumb        = new Thumb { HorizontalAlignment = HorizontalAlignment.Left,  VerticalAlignment = VerticalAlignment.Stretch, Cursor = Cursors.SizeWE };
        _topThumb         = new Thumb { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Top,    Cursor = Cursors.SizeNS };
        _rightThumb       = new Thumb { HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Stretch, Cursor = Cursors.SizeWE };
        _bottomThumb      = new Thumb { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Bottom, Cursor = Cursors.SizeNS };
        _lefTopThumb      = new Thumb { HorizontalAlignment = HorizontalAlignment.Left,  VerticalAlignment = VerticalAlignment.Top,    Cursor = Cursors.SizeNWSE };
        _rightTopThumb    = new Thumb { HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top,    Cursor = Cursors.SizeNESW };
        _rightBottomThumb = new Thumb { HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom, Cursor = Cursors.SizeNWSE };
        _leftbottomThumb  = new Thumb { HorizontalAlignment = HorizontalAlignment.Left,  VerticalAlignment = VerticalAlignment.Bottom, Cursor = Cursors.SizeNESW };

        _grid = new Grid();
        _grid.Children.Add(_leftThumb);
        _grid.Children.Add(_topThumb);
        _grid.Children.Add(_rightThumb);
        _grid.Children.Add(_bottomThumb);
        _grid.Children.Add(_lefTopThumb);
        _grid.Children.Add(_rightTopThumb);
        _grid.Children.Add(_rightBottomThumb);
        _grid.Children.Add(_leftbottomThumb);
        AddVisualChild(_grid);

        foreach (Thumb thumb in _grid.Children)
        {
            const int thumbSize = 12;
            if (thumb.HorizontalAlignment == HorizontalAlignment.Stretch)
            {
                thumb.Width  = double.NaN;
                thumb.Margin = new Thickness(thumbSize, 0, thumbSize, 0);
            }
            else
            {
                thumb.Width = thumbSize;
            }
            if (thumb.VerticalAlignment == VerticalAlignment.Stretch)
            {
                thumb.Height = double.NaN;
                thumb.Margin = new Thickness(0, thumbSize, 0, thumbSize);
            }
            else
            {
                thumb.Height = thumbSize;
            }
            thumb.Background = System.Windows.Media.Brushes.Green;
            thumb.Template = new ControlTemplate(typeof(Thumb))
            {
                VisualTree = GetFactory(new SolidColorBrush(Colors.Transparent))
            };
            thumb.DragDelta += Thumb_DragDelta;
        }
    }

    protected override Visual GetVisualChild(int index) => _grid;
    protected override int VisualChildrenCount => 1;

    protected override Size ArrangeOverride(Size finalSize)
    {
        _grid.Arrange(new Rect(
            new Point(
                -(_window.RenderSize.Width  - finalSize.Width)  / 2,
                -(_window.RenderSize.Height - finalSize.Height) / 2),
            _window.RenderSize));
        return finalSize;
    }

    private void Thumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (!IsEnabled) return;

        var thumb = sender as FrameworkElement;
        var c = _window;

        // Island 模式：只允许纯左/纯右 thumb（HorizontalAlignment != Stretch 且 VerticalAlignment == Stretch）
        // 上下边、四角全部忽略
        if (IslandMode)
        {
            bool isHorizontalOnly =
                thumb!.VerticalAlignment   == VerticalAlignment.Stretch &&
                thumb.HorizontalAlignment  != HorizontalAlignment.Stretch;

            if (!isHorizontalOnly) return;

            double left, width;
            if (thumb.HorizontalAlignment == HorizontalAlignment.Left)
            {
                left  = c.Left + e.HorizontalChange;
                width = c.Width - e.HorizontalChange;
            }
            else
            {
                left  = c.Left;
                width = c.Width + e.HorizontalChange;
            }

            // ★ 最小宽度阈值 60px
            if (width >= IslandMinWidth)
            {
                c.Left  = left;
                c.Width = width;
                // 同步保存到配置，方便下次记忆
                // （_settingsMgr 不在这里，改宽度后由 ViewModel 监听 SizeChanged 处理即可）
            }
            return;
        }

        // 普通模式：原逻辑
        double l, t, w, h;
        if (thumb!.HorizontalAlignment == HorizontalAlignment.Left)
        {
            l = c.Left + e.HorizontalChange;
            w = c.Width - e.HorizontalChange;
        }
        else
        {
            l = c.Left;
            w = c.Width + e.HorizontalChange;
        }
        if (thumb.HorizontalAlignment != HorizontalAlignment.Stretch)
        {
            if (w > 0) { c.Left = l; c.Width = w; }
        }

        if (thumb.VerticalAlignment == VerticalAlignment.Top)
        {
            t = c.Top + e.VerticalChange;
            h = c.Height - e.VerticalChange;
        }
        else
        {
            t = c.Top;
            h = c.Height + e.VerticalChange;
        }
        if (thumb.VerticalAlignment != VerticalAlignment.Stretch)
        {
            if (h > 0) { c.Top = t; c.Height = h; }
        }
    }

    FrameworkElementFactory GetFactory(Brush back)
    {
        var fef = new FrameworkElementFactory(typeof(Rectangle));
        fef.SetValue(Rectangle.FillProperty, back);
        return fef;
    }
}
