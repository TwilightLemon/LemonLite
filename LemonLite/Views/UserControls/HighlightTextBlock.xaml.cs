using LemonLite.Shaders.Impl;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LemonLite.Views.UserControls;

public partial class HighlightTextBlock : UserControl
{
    private readonly ProgresiveHighlightEffect _effect;

    static HighlightTextBlock()
    {
        // 重写继承的文本属性元数据，以便在属性更改时更新文本裁剪
        FontFamilyProperty.OverrideMetadata(typeof(HighlightTextBlock),
            new FrameworkPropertyMetadata(SystemFonts.MessageFontFamily, OnTextPropertyChanged));
        FontSizeProperty.OverrideMetadata(typeof(HighlightTextBlock),
            new FrameworkPropertyMetadata(14.0, OnTextPropertyChanged));
        FontWeightProperty.OverrideMetadata(typeof(HighlightTextBlock),
            new FrameworkPropertyMetadata(FontWeights.Normal, OnTextPropertyChanged));
        FontStyleProperty.OverrideMetadata(typeof(HighlightTextBlock),
            new FrameworkPropertyMetadata(FontStyles.Normal, OnTextPropertyChanged));
        FontStretchProperty.OverrideMetadata(typeof(HighlightTextBlock),
            new FrameworkPropertyMetadata(FontStretches.Normal, OnTextPropertyChanged));
        ForegroundProperty.OverrideMetadata(typeof(HighlightTextBlock),
            new FrameworkPropertyMetadata(Brushes.Black, OnForegroundChanged));
    }

    public HighlightTextBlock()
    {
        InitializeComponent();
        _effect = new()
        {
            HighlightColor = HighlightColor,
            HighlightPos = HighlightPos,
            HighlightWidth = HighlightWidth
        };
        PART_Rectangle.Effect = _effect;
        Loaded += (_, _) => UpdateTextClip();
    }

    #region Text

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(HighlightTextBlock),
            new PropertyMetadata(string.Empty, OnTextPropertyChanged));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    #endregion

    #region HighlightPos

    /// <summary>
    /// 高光中心位置（0~1，允许动画越界）
    /// </summary>
    public static readonly DependencyProperty HighlightPosProperty =
        DependencyProperty.Register(
            nameof(HighlightPos),
            typeof(double),
            typeof(HighlightTextBlock),
            new PropertyMetadata(0.0, OnHighlightPosChanged));

    public double HighlightPos
    {
        get => (double)GetValue(HighlightPosProperty);
        set => SetValue(HighlightPosProperty, value);
    }

    private static void OnHighlightPosChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is HighlightTextBlock c)
        {
            c._effect.HighlightPos = (double)e.NewValue;
        }
    }

    #endregion

    #region HighlightWidth

    /// <summary>
    /// 高光宽度 0~1
    /// </summary>
    public static readonly DependencyProperty HighlightWidthProperty =
        DependencyProperty.Register(
            nameof(HighlightWidth),
            typeof(double),
            typeof(HighlightTextBlock),
            new PropertyMetadata(0.4, OnHighlightWidthChanged));

    public double HighlightWidth
    {
        get => (double)GetValue(HighlightWidthProperty);
        set => SetValue(HighlightWidthProperty, value);
    }

    private static void OnHighlightWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is HighlightTextBlock c)
        {
            c._effect.HighlightWidth = (double)e.NewValue;
        }
    }

    #endregion

    #region HighlightColor

    /// <summary>
    /// 高光颜色
    /// </summary>
    public static readonly DependencyProperty HighlightColorProperty =
        DependencyProperty.Register(
            nameof(HighlightColor),
            typeof(Color),
            typeof(HighlightTextBlock),
            new PropertyMetadata(Color.FromArgb(240, 230, 242, 255), OnHighlightColorChanged));

    public Color HighlightColor
    {
        get => (Color)GetValue(HighlightColorProperty);
        set => SetValue(HighlightColorProperty, value);
    }

    private static void OnHighlightColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is HighlightTextBlock c)
        {
            c._effect.HighlightColor = (Color)e.NewValue;
        }
    }

    #endregion



    public bool UseAdditive
    {
        get { return (bool)GetValue(UseAdditiveProperty); }
        set { SetValue(UseAdditiveProperty, value); }
    }

    public static readonly DependencyProperty UseAdditiveProperty =
        DependencyProperty.RegisterAttached(nameof(UseAdditive), typeof(bool), typeof(HighlightTextBlock), 
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.Inherits, OnUseAdditiveChanged));

    public static bool GetUseAdditive(DependencyObject obj) => (bool)obj.GetValue(UseAdditiveProperty);
    public static void SetUseAdditive(DependencyObject obj, bool value) => obj.SetValue(UseAdditiveProperty, value);

    private static void OnUseAdditiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is HighlightTextBlock c)
        {
            c._effect.UseAdditive = (bool)e.NewValue;
        }
    }

    private static void OnForegroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is HighlightTextBlock control)
        {
            control.PART_Rectangle.Fill = e.NewValue as Brush;
        }
    }

    private static void OnTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is HighlightTextBlock control)
        {
            control.UpdateTextClip();
        }
    }

    private void UpdateTextClip()
    {
        if (Text == null)
        {
            PART_Rectangle.Clip = null;
            Width = 0;
            Height = 0;
            return;
        }

        var formattedText = new FormattedText(
            Text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(FontFamily, FontStyle, FontWeight, FontStretch),
            FontSize,
            Brushes.Black,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);

        var geometry = formattedText.BuildGeometry(new Point(0, 0));
        var width = formattedText.WidthIncludingTrailingWhitespace;
        var height = formattedText.Height;

        PART_Rectangle.Clip = geometry;
        PART_Rectangle.Width = width;
        PART_Rectangle.Height = height;

        Width = width;
        Height = height;
    }
}
