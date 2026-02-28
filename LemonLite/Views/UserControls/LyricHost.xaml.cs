using EleCho.WpfSuite;
using LemonLite.Services;
using Lyricify.Lyrics.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Windows.Media.Control;
using ScrollViewer = LemonLite.Utils.ScrollViewer;

namespace LemonLite.Views.UserControls;

public class LyricLineData(ILineInfo lineInfo)
{
    public ILineInfo LineInfo { get; } = lineInfo;
    public string? TranslationText { get; set; }
    public SyllableLineInfo? RomajiSyllables { get; set; }
    public string? PlainRomaji { get; set; }
}

public class LyricItemsControl : ItemsControl
{
    public ScrollViewer? InternalScrollViewer { get; private set; }

    public double MainLrcFontSize { get; set; } = 24;
    public double TransLrcFontSize { get; set; } = 18;
    public bool ShowTranslation { get; set; } = true;
    public bool ShowRomaji { get; set; } = true;
    public LyricLineData? CurrentItem { get; set; }
    public Thickness LyricSpacing { get; set; } = new(0, 0, 0, 36);

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        InternalScrollViewer = GetTemplateChild("PART_ScrollViewer") as ScrollViewer;
    }

    protected override DependencyObject GetContainerForItemOverride()
    {
        return new SelectiveLyricLine();
    }

    protected override bool IsItemItsOwnContainerOverride(object item)
    {
        return item is SelectiveLyricLine;
    }

    protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
    {
        base.PrepareContainerForItemOverride(element, item);
        if (element is SelectiveLyricLine container && item is LyricLineData data)
        {
            container.Margin = LyricSpacing;
            container.LoadData(data, MainLrcFontSize, TransLrcFontSize, ShowTranslation, ShowRomaji);
            if (data == CurrentItem && container.LyricLine != null)
            {
                container.LyricLine.IsCurrent = true;
            }
        }
    }

    protected override void ClearContainerForItemOverride(DependencyObject element, object item)
    {
        base.ClearContainerForItemOverride(element, item);
        if (element is SelectiveLyricLine container)
        {
            container.ClearData();
        }
    }
}

public sealed class SelectiveLyricLine : Border
{
    public LyricLineControl? LyricLine { get; private set; }

    public SelectiveLyricLine()
    {
        Background = Brushes.Transparent;
        CornerRadius = new(12);
        Padding = new(8, 4, 8, 4);
        MouseEnter += SelectiveLyricLine_MouseEnter;
        MouseLeave += SelectiveLyricLine_MouseLeave;
        MouseDown += SelectiveLyricLine_MouseDown;
    }

    public void LoadData(LyricLineData data, double mainFontSize, double transFontSize, bool showTranslation, bool showRomaji)
    {
        LyricLineControl lrc;
        if (data.LineInfo is SyllableLineInfo syllable)
        {
            lrc = new LyricLineControl(syllable, mainFontSize);
        }
        else if (data.LineInfo is LineInfo pure)
        {
            lrc = new LyricLineControl(pure, mainFontSize);
            lrc.FontSize = transFontSize;
        }
        else return;

        if (data.TranslationText != null)
            lrc.TranslationLrc.Text = data.TranslationText;
        lrc.TranslationLrc.Visibility = showTranslation ? Visibility.Visible : Visibility.Collapsed;

        if (data.RomajiSyllables != null)
            lrc.LoadRomajiLrc(data.RomajiSyllables);
        else if (data.PlainRomaji != null)
            lrc.LoadPlainRomaji(data.PlainRomaji);
        lrc.RomajiLrcContainer.Visibility = showRomaji ? Visibility.Visible : Visibility.Collapsed;

        LyricLine = lrc;
        Child = lrc;
    }

    public void ClearData()
    {
        LyricLine = null;
        Child = null;
    }

    private async void SelectiveLyricLine_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (LyricLine?.MainLineInfo?.StartTime is int startTime)
        {
            var smtc = App.Services.GetRequiredService<SmtcService>().SmtcListener;
            await smtc.SetPosition(TimeSpan.FromMilliseconds(startTime));
            if (smtc.GetPlaybackStatus() != GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)
            {
                await smtc.PlayOrPause();
            }
        }
    }

    private void SelectiveLyricLine_MouseLeave(object sender, MouseEventArgs e)
    {
        if (Background is SolidColorBrush)
            Background.BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation(Colors.Transparent, TimeSpan.FromMilliseconds(200)));

        if (LyricLine != null && !LyricLine.IsCurrent)
            LyricLine.SetActiveState(false, false);
    }

    private void SelectiveLyricLine_MouseEnter(object sender, MouseEventArgs e)
    {
        var color = ((SolidColorBrush)Application.Current.FindResource("MaskColor")).Color;
        var brush = new SolidColorBrush(Colors.Transparent);
        var da = new ColorAnimation(color, TimeSpan.FromMilliseconds(200));
        Background = brush;
        brush.BeginAnimation(SolidColorBrush.ColorProperty, da);
        if (LyricLine != null && !LyricLine.IsCurrent)
            LyricLine.SetActiveState(true);
    }
}

public partial class LyricHost : UserControl
{
    private const double ItemsPresenterMargin = 300;

    public LyricHost()
    {
        InitializeComponent();
        SizeChanged += SimpleLyricView_SizeChanged;
        LrcItemsControl.Loaded += LrcItemsControl_Loaded;
    }

    private void LrcItemsControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (_scrollViewer != null) return;
        _scrollViewer = LrcItemsControl.InternalScrollViewer;
        if (_scrollViewer != null)
        {
            _scrollViewer.PreviewMouseWheel += ScrollViewer_PreviewMouseWheel;
            _scrollViewer.PreviewMouseDown += ScrollViewer_PreviewMouseDown;
        }
        UpdateItemsPresenterMargin();
    }

    private static readonly DependencyProperty ScrollAnimationOffsetProperty =
        DependencyProperty.Register(
            nameof(ScrollAnimationOffset),
            typeof(double),
            typeof(LyricHost),
            new PropertyMetadata(0.0, OnScrollAnimationOffsetChanged));

    private double ScrollAnimationOffset
    {
        get => (double)GetValue(ScrollAnimationOffsetProperty);
        set => SetValue(ScrollAnimationOffsetProperty, value);
    }

    private static void OnScrollAnimationOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LyricHost host && host._scrollViewer != null)
        {
            host._scrollViewer.ScrollToVerticalOffset((double)e.NewValue);
        }
    }

    private ScrollViewer? _scrollViewer;
    private ItemsPresenter? _itemsPresenter;
    private List<LyricLineData> _items = [];
    private LyricLineData? _currentItem;
    private bool _isPureLrc = false;
    private DateTime _interruptedTime;
    private bool _isLoading = false;

    private async Task WaitToScroll()
    {
        await Task.Delay(100);
        Dispatcher.Invoke(ScrollToCurrent);
    }
    private double mainLrcFontSize = 24, transLrcFontSize = 18;
    public void ApplyFontSize(double size, double scale)
    {
        mainLrcFontSize = size;
        transLrcFontSize = scale;
        this.FontSize = size * scale;
        LrcItemsControl.MainLrcFontSize = size;
        LrcItemsControl.TransLrcFontSize = scale;
        ForEachRealizedContainer(line =>
        {
            if (line.LyricLine != null)
            {
                foreach (HighlightTextBlock tb in line.LyricLine.MainLrcContainer.Children)
                {
                    tb.FontSize = size;
                }
            }
        });
        _ = WaitToScroll();
    }
    private bool isShowTranslation = true, isShowRomaji = true;
    public void SetShowTranslation(bool show)
    {
        isShowTranslation = show;
        LrcItemsControl.ShowTranslation = show;
        ForEachRealizedContainer(line =>
        {
            if (line.LyricLine != null)
                line.LyricLine.TranslationLrc.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        });
        _ = WaitToScroll();
    }
    public void SetShowRomaji(bool show)
    {
        isShowRomaji = show;
        LrcItemsControl.ShowRomaji = show;
        ForEachRealizedContainer(line =>
        {
            if (line.LyricLine != null)
                line.LyricLine.RomajiLrcContainer.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        });
        _ = WaitToScroll();
    }
    public void Clear()
    {
        StopScrollAnimation();
        _currentItem = null;
        LrcItemsControl.CurrentItem = null;

        // Reset scroll position and flush layout BEFORE clearing items
        // to prevent VirtualizingStackPanel.OnAnchorOperation from accessing
        // containers that have been disconnected from the visual tree.
        _scrollViewer?.ScrollToVerticalOffset(0);
        LrcItemsControl.UpdateLayout();

        _items = [];
        LrcItemsControl.ItemsSource = null;
    }

    private Thickness lyricSpacing = new(0, 0, 0, 36);
    public void Load(LyricsData lyricsData, LyricsData? trans = null, LyricsData? romaji = null, bool isPureLrc = false)
    {
        _isLoading = true;
        _isPureLrc = isPureLrc;
        Clear();

        LrcItemsControl.MainLrcFontSize = mainLrcFontSize;
        LrcItemsControl.TransLrcFontSize = transLrcFontSize;
        LrcItemsControl.ShowTranslation = isShowTranslation;
        LrcItemsControl.ShowRomaji = isShowRomaji;
        LrcItemsControl.LyricSpacing = lyricSpacing;

        var items = new List<LyricLineData>();
        foreach (var line in lyricsData.Lines)
        {
            if (line is SyllableLineInfo or LineInfo)
            {
                items.Add(new LyricLineData(line));
            }
        }

        if (trans is { Lines: not null })
        {
            foreach (var data in items)
            {
                var transLrc = trans.Lines.FirstOrDefault(a => a.StartTime >= data.LineInfo.StartTime - 10);
                if (transLrc != null && transLrc.Text != "//")
                    data.TranslationText = transLrc.Text;
            }
        }
        if (romaji is { Lines: not null })
        {
            foreach (var data in items)
            {
                var romajiLrc = romaji.Lines.FirstOrDefault(a => a.StartTime >= data.LineInfo.StartTime - 10);
                if (romajiLrc is SyllableLineInfo roma)
                    data.RomajiSyllables = roma;
                else if (romajiLrc is LineInfo pure)
                    data.PlainRomaji = pure.Text;
            }
        }

        _items = items;
        LrcItemsControl.ItemsSource = _items;
        _isLoading = false;
    }

    public void UpdateTime(int ms)
    {
        if (_isLoading || _scrollViewer == null) return;

        if (_currentItem != null)
        {
            var container = GetContainerForItem(_currentItem);
            container?.LyricLine?.UpdateTime(ms);
        }

        //从上一条结束到本条结束都是当前歌词时间，目的是本条歌词结束就跳转到下一个
        LyricLineData? target = null;
        if (!_isPureLrc)
        {
            LyricLineData? lastItem = null;
            foreach (var cur in _items)
            {
                if (string.IsNullOrEmpty(cur.LineInfo.Text)) continue;
                if ((lastItem?.LineInfo.EndTime ?? cur.LineInfo.StartTime) <= ms && cur.LineInfo.EndTime >= ms)
                {
                    target = cur;
                    break;
                }
                lastItem = cur;
            }
        }
        else
        {
            target = _items.LastOrDefault(a => a.LineInfo.StartTime <= ms);
        }

        //next found. 对于LyricPage希望准确使用当前时间来定位歌词
        if (target != null)
        {
            var line = target.LineInfo;
            if (line.StartTime > ms || (line.EndTime ?? int.MaxValue) < ms) return;

            if (target == _currentItem) return;
            if (_currentItem != null)
            {
                var prevContainer = GetContainerForItem(_currentItem);
                if (prevContainer?.LyricLine != null)
                    prevContainer.LyricLine.IsCurrent = false;
            }
            _currentItem = target;
            LrcItemsControl.CurrentItem = target;

            var currentContainer = GetContainerForItem(_currentItem);
            if (currentContainer?.LyricLine != null)
                currentContainer.LyricLine.IsCurrent = true;

            ScrollToCurrent();
        }
    }

    private SelectiveLyricLine? GetContainerForItem(LyricLineData item)
    {
        return LrcItemsControl.ItemContainerGenerator.ContainerFromItem(item) as SelectiveLyricLine;
    }

    private void ScrollToCurrent()
    {
        if (_isLoading || _scrollViewer == null) return;
        if ((DateTime.Now - _interruptedTime).TotalSeconds < 5) return;
        try
        {
            if (_currentItem == null) return;
            var currentControl = GetContainerForItem(_currentItem);

            // If the container is not realized, bring it into view
            if (currentControl == null)
            {
                int index = _items.IndexOf(_currentItem);
                if (index >= 0)
                {
                    var panel = FindVisualChild<VirtualizingStackPanel>(LrcItemsControl);
                    panel?.BringIndexIntoViewPublic(index);
                    LrcItemsControl.UpdateLayout();
                    currentControl = GetContainerForItem(_currentItem);
                }
                if (currentControl == null) return;
            }

            // 计算目标滚动位置
            GeneralTransform gf = currentControl.TransformToVisual(_scrollViewer);
            Point p = gf.Transform(new Point(0, 0));
            double targetOffset = _scrollViewer.VerticalOffset + p.Y
                - (_scrollViewer.ActualHeight / 2d) + currentControl.ActualHeight / 2d;
            targetOffset = Math.Max(0, Math.Min(targetOffset, _scrollViewer.ScrollableHeight));

            double currentOffset = _scrollViewer.VerticalOffset;
            double scrollDelta = Math.Abs(targetOffset - currentOffset);

            if (scrollDelta < 1) return;

            Debug.WriteLine($"Animate lyricHost scrolling: {targetOffset}");

            var animation = new DoubleAnimation
            {
                To = targetOffset,
                Duration = TimeSpan.FromMilliseconds(500),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            this.BeginAnimation(ScrollAnimationOffsetProperty, animation);
        }
        catch { }
    }

    private void StopScrollAnimation()
    {
        this.BeginAnimation(ScrollAnimationOffsetProperty, null);
    }

    private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        _interruptedTime = DateTime.Now;
        StopScrollAnimation();
    }
    private void SimpleLyricView_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateItemsPresenterMargin();
        ScrollToCurrent();
    }

    private void ScrollViewer_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        _interruptedTime = DateTime.MinValue;
    }

    private void ForEachRealizedContainer(Action<SelectiveLyricLine> action)
    {
        for (int i = 0; i < _items.Count; i++)
        {
            if (LrcItemsControl.ItemContainerGenerator.ContainerFromIndex(i) is SelectiveLyricLine line)
            {
                action(line);
            }
        }
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T found)
                return found;
            var result = FindVisualChild<T>(child);
            if (result != null)
                return result;
        }
        return null;
    }

    private void UpdateItemsPresenterMargin()
    {
        if (_scrollViewer == null)
            return;

        _itemsPresenter ??= _scrollViewer.Content as ItemsPresenter ?? FindVisualChild<ItemsPresenter>(_scrollViewer);
        if (_itemsPresenter == null)
            return;

        var maxMargin = Math.Max(0, (_scrollViewer.ActualHeight / 2d) - 1);
        var margin = Math.Min(ItemsPresenterMargin, maxMargin);
        _itemsPresenter.Margin = new Thickness(0, margin, 0, margin);
    }
}
