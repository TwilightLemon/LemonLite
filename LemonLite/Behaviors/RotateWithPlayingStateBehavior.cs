using Microsoft.Xaml.Behaviors;
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace LemonLite.Behaviors;

public class RotateWithPlayingStateBehavior:Behavior<RotateTransform>
{


    public bool IsPlaying
    {
        get { return (bool)GetValue(IsPlayingProperty); }
        set { SetValue(IsPlayingProperty, value); }
    }

    // Using a DependencyProperty as the backing store for IsPlaying.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IsPlayingProperty =
        DependencyProperty.Register(nameof(IsPlaying), typeof(bool), 
            typeof(RotateWithPlayingStateBehavior), 
            new PropertyMetadata(false,OnIsPlayingChanged));



    public bool IsEnabled
    {
        get { return (bool)GetValue(IsEnabledProperty); }
        set { SetValue(IsEnabledProperty, value); }
    }

    // Using a DependencyProperty as the backing store for IsEnabled.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.Register(nameof(IsEnabled), typeof(bool), 
            typeof(RotateWithPlayingStateBehavior), 
            new PropertyMetadata(false,OnIsPlayingChanged));



    private static void OnIsPlayingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RotateWithPlayingStateBehavior behavior)
        {
            bool isPlaying = (bool)e.NewValue;
            behavior.ControlRotationAnimation(isPlaying);
        }
    }

    private Storyboard? rotationStoryboard;

    private void ControlRotationAnimation(bool play)
    {
        if (rotationStoryboard == null )
        {
            if (!IsEnabled || AssociatedObject == null) return;
            rotationStoryboard = new();
            DoubleAnimation da = new(0, 360, TimeSpan.FromSeconds(30))
            {
                RepeatBehavior = RepeatBehavior.Forever
            };
            Storyboard.SetTarget(da, AssociatedObject);
            Storyboard.SetTargetProperty(da, new PropertyPath("(RotateTransform.Angle)"));
            rotationStoryboard.Children.Add(da);
            rotationStoryboard.Freeze();
            rotationStoryboard.Begin();
        }
        else
        {
            if (!IsEnabled)
            {
                rotationStoryboard.Pause();
                return;
            }
            if (play)
            {
                rotationStoryboard.Resume();
            }
            else
            {
                rotationStoryboard.Pause();
            }
        }
    }
}
