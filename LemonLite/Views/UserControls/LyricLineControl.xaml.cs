using Lyricify.Lyrics.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace LemonLite.Views.UserControls;

/// <summary>
/// LyricLineControl.xaml 的交互逻辑
/// </summary>
public partial class LyricLineControl : UserControl
{
    private readonly Dictionary<ISyllableInfo, HighlightTextBlock> mainSyllableLrcs = [];
    private readonly Dictionary<ISyllableInfo, TextBlock> romajiSyllableLrcs = [];
    private const int InActiveLrcBlurRadius = 6;
    private int ActiveLrcLiftupHeight => (int)(-FontSize / 4);
    private double AverageWordDuration = 0.0;
    public SyllableLineInfo? RomajiSyllables { get; private set; }
    public ILineInfo? MainLineInfo { get; private set; }
    public Dictionary<ISyllableInfo, HighlightTextBlock> MainSyllableLrcs => mainSyllableLrcs;

    private bool _isPlainLrc = false;

    //reserved for desktop lyric view
    public LyricLineControl()
    {
        InitializeComponent();
        //no blur effect
    }

    public LyricLineControl(SyllableLineInfo info)
    {
        InitializeComponent();
        MainLineInfo = info;
        Effect = new BlurEffect() { Radius = InActiveLrcBlurRadius };
        LoadMainLrc(info.Syllables);
    }

    public LyricLineControl(LineInfo info)
    {
        InitializeComponent();
        MainLineInfo = info;
        Effect = new BlurEffect() { Radius = InActiveLrcBlurRadius };
        _isPlainLrc = true;
        LoadPlainLrc(info.Text);
    }

    public void ClearAll()
    {
        MainLrcContainer.Children.Clear();
        RomajiLrcContainer.Children.Clear();
        mainSyllableLrcs.Clear();
        romajiSyllableLrcs.Clear();
        mainSyllableAnimated.Clear();
        TranslationLrc.Text = string.Empty;
    }
    public void LoadPlainLrc(string lrc, double fontSize = 22)
    {
        MainLrcContainer.Children.Clear();
        var tb = new HighlightTextBlock()
        {
            Text = lrc,
            TextWrapping = TextWrapping.Wrap,
            FontSize = fontSize,
            HighlightPos=-0.5
        };
        //tb.SetResourceReference(ForegroundProperty, "InActiveLrcForeground");
        MainLrcContainer.Children.Add(tb);
    }

    public void LoadPlainRomaji(string? romaji)
    {
        if (romaji == null) return;
        RomajiLrcContainer.Children.Clear();
        RomajiLrcContainer.Children.Add(new TextBlock()
        {
            Text = romaji,
            TextWrapping = TextWrapping.Wrap
        });
    }

    public void LoadMainLrc(List<ISyllableInfo> words, double fontSize = 22)
    {
        MainLrcContainer.Children.Clear();
        mainSyllableLrcs.Clear();
        ClearHighlighter();

        AverageWordDuration = words.Average(w => w.Duration);

        foreach (var word in words)
        {
            var textBlock = new HighlightTextBlock
            {
                Text = word.Text,
                FontSize = fontSize,
                HighlightPos = -0.5,
                HighlightWidth=0.32,
                RenderTransform = new TranslateTransform()
            };
            textBlock.SetResourceReference(HighlightTextBlock.HighlightColorProperty, "ActiveLrcForegroundColor");

            MainLrcContainer.Children.Add(textBlock);
            mainSyllableLrcs[word] = textBlock;
        }
    }

    public void LoadRomajiLrc(SyllableLineInfo words)
    {
        RomajiLrcContainer.Children.Clear();
        romajiSyllableLrcs.Clear();
        RomajiSyllables = words;
        foreach (var word in words.Syllables)
        {
            var textBlock = new TextBlock
            {
                Text = word.Text,
                TextTrimming = TextTrimming.None
            };
            RomajiLrcContainer.Children.Add(textBlock);
            romajiSyllableLrcs[word] = textBlock;
        }
    }

    private readonly Dictionary<ISyllableInfo, bool> mainSyllableAnimated = new();

    public void UpdateTime(int ms)
    {
        if (_isPlainLrc)
        {
            //do nothing 
        }
        else
        {
            foreach (var kvp in mainSyllableLrcs)
            {
                var syllable = kvp.Key;
                var textBlock = kvp.Value;

                if (syllable.EndTime < ms)
                {
                    // 已经过了，直接填满
                    textBlock.HighlightPos = 1;
                    mainSyllableAnimated[syllable] = true;
                    //lift-up: 如果动画正在执行则不强制设置，确保歌词流畅性
                    if (textBlock.RenderTransform is TranslateTransform trans && !trans.HasAnimatedProperties)
                    {
                        trans.Y = ActiveLrcLiftupHeight;
                    }
                }
                else if (syllable.StartTime > ms)
                {
                    // 还没到，保持未填充
                    textBlock.BeginAnimation(HighlightTextBlock.HighlightPosProperty, null);
                    textBlock.HighlightPos = -0.5;
                    mainSyllableAnimated[syllable] = false;

                    //clear lift-up
                    if (textBlock.RenderTransform is TranslateTransform trans)
                    {
                        trans.BeginAnimation(TranslateTransform.YProperty, null);
                        trans.Y = 0;
                    }
                }
                else
                {
                    // 正在进行，判断是否需要启动动画
                    if (!mainSyllableAnimated.TryGetValue(syllable, out var animated) || !animated)
                    {
                        mainSyllableAnimated[syllable] = true;

                        var liftupAni = new DoubleAnimation(0, ActiveLrcLiftupHeight,
                                    TimeSpan.FromMilliseconds(AverageWordDuration * 1.5))
                        {
                            EasingFunction = new ExponentialEase()
                        };

                        // HighlightPos animation: from -0.5 to 1
                        var duration = TimeSpan.FromMilliseconds(syllable.Duration);
                        var highlightAnim = new DoubleAnimation(-0.5, 1, new Duration(duration));
                        textBlock.BeginAnimation(HighlightTextBlock.HighlightPosProperty, highlightAnim);

                        //lift-up animation
                        if (textBlock.RenderTransform is TranslateTransform trans)
                        {
                            trans.BeginAnimation(TranslateTransform.YProperty, liftupAni);
                        }
                    }
                }
            }

            // Romaji歌词 颜色渐变动画
            foreach (var kvp in romajiSyllableLrcs)
            {
                var syllable = kvp.Key;
                var textBlock = kvp.Value;
                if ((syllable.StartTime) <= ms && (syllable.EndTime +200) >= ms)
                {
                    //textBlock.SetResourceReference(ForegroundProperty, "HighlightThemeColor");
                    if (textBlock.Tag is not true)
                    {
                        var fontColor = ((SolidColorBrush)FindResource("InActiveLrcForeground")).Color;
                        var highlightColor = ((SolidColorBrush)FindResource("HighlightThemeColor")).Color;
                        var brush = new SolidColorBrush();
                        textBlock.Foreground = brush;
                        var ani = new ColorAnimationUsingKeyFrames
                        {
                            KeyFrames =
                            [
                                new EasingColorKeyFrame(highlightColor, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(0))),
                                new EasingColorKeyFrame(highlightColor, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(syllable.Duration))),
                                new EasingColorKeyFrame(fontColor, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(syllable.Duration+1000)))
                            ]
                        };
                        brush.BeginAnimation(SolidColorBrush.ColorProperty, ani);
                        textBlock.Tag = true;
                    }
                }
                else
                {
                    textBlock.Tag = false;
                }
            }
        }
    }

    public void ClearHighlighter(bool animated = false)
    {
        mainSyllableAnimated.Clear();
        foreach (var lrc in mainSyllableLrcs)
        {
            // Reset HighlightPos to initial state
            lrc.Value.BeginAnimation(HighlightTextBlock.HighlightPosProperty, null);

            //fade animation
            if (animated)
            {
                lrc.Value.HighlightPos = 1;
                lrc.Value.BeginAnimation(HighlightTextBlock.HighlightIntensityProperty, new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300)));
            }
            else lrc.Value.HighlightPos = -0.5;

            //clear lift-up
            if (lrc.Value.RenderTransform is TranslateTransform trans)
            {
                trans.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(0,TimeSpan.FromMilliseconds(200)));
            }
        }
    }

    public void SetActiveState(bool isActive,bool inactiveAnimated=true)
    {
        var control = this;
        if (isActive)
        {   //Active
            var blur = new BlurEffect() { Radius = InActiveLrcBlurRadius };
            control.Effect = blur;
            var da = new DoubleAnimation(InActiveLrcBlurRadius, 0, TimeSpan.FromMilliseconds(300));
            blur.BeginAnimation(BlurEffect.RadiusProperty, da);

            if (control._isPlainLrc)
            {
                if (control.MainLrcContainer.Children[0] is HighlightTextBlock tb)
                {
                    tb.HighlightPos = inactiveAnimated ? 1 : -0.5;
                }
            }
            else
            {
                // Reset HighlightPos to initial state for all syllables
                foreach (var lrc in control.mainSyllableLrcs)
                {
                    lrc.Value.HighlightPos = -0.5;
                    lrc.Value.BeginAnimation(HighlightTextBlock.HighlightIntensityProperty, null);
                }
            }
        }
        else
        {
            var blur = new BlurEffect() { Radius = 0 };
            control.Effect = blur;
            blur.BeginAnimation(BlurEffect.RadiusProperty, new DoubleAnimation(0, InActiveLrcBlurRadius, TimeSpan.FromMilliseconds(300)));
            if (control._isPlainLrc)
            {
                if (control.MainLrcContainer.Children[0] is HighlightTextBlock tb)
                {
                    tb.HighlightPos = -0.5;
                }
            }
            else control.ClearHighlighter(inactiveAnimated);
        }
    }



    public Brush CustomHighlightColorBrush
    {
        get { return (Brush)GetValue(CustomHighlightColorBrushProperty); }
        set { SetValue(CustomHighlightColorBrushProperty, value); }
    }

    public static readonly DependencyProperty CustomHighlightColorBrushProperty =
        DependencyProperty.Register(nameof(CustomHighlightColorBrush), typeof(Brush), typeof(LyricLineControl), new PropertyMetadata(null, OnCustomHighlightColorBrushChanged));

    private static void OnCustomHighlightColorBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LyricLineControl control && e.NewValue is SolidColorBrush brush)
        {
            control.MainLrcContainer.Resources["ActiveLrcForegroundColor"] = brush.Color;
        }
    }

    public bool IsCurrent
    {
        get { return (bool)GetValue(IsCurrentProperty); }
        set { SetValue(IsCurrentProperty, value); }
    }

    // Using a DependencyProperty as the backing store for IsCurrent.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IsCurrentProperty =
        DependencyProperty.Register("IsCurrent", typeof(bool), typeof(LyricLineControl), new PropertyMetadata(false, OnIsCurrentChanged));

    private static void OnIsCurrentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LyricLineControl control)
        {
            control.SetActiveState((bool)e.NewValue);
        }
    }
}
