using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LemonLite.Utils
{
    public class TopCutBorder : Border
    {
        public static readonly DependencyProperty TopCutRadiusProperty =
            DependencyProperty.Register(nameof(TopCutRadius), typeof(double), typeof(TopCutBorder),
                new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender));

        public double TopCutRadius
        {
            get => (double)GetValue(TopCutRadiusProperty);
            set => SetValue(TopCutRadiusProperty, value);
        }

        protected override void OnRender(DrawingContext dc)
        {
            if(TopCutRadius<=0.1d)
            {
                base.OnRender(dc);
                return;
            }
            var strokeThickness = Math.Max(0d, BorderThickness.Left);
            var halfStroke = strokeThickness / 2d;
            var width = Math.Max(0d, ActualWidth - strokeThickness);
            var height = Math.Max(0d, ActualHeight - strokeThickness);

            if (width <= 0d || height <= 0d)
            {
                return;
            }

            var rect = new Rect(halfStroke, halfStroke, width, height);
            var topRadius = ClampRadius(TopCutRadius, rect.Width / 2d, rect.Height);
            var bottomLeft = ClampRadius(CornerRadius.BottomLeft, rect.Width / 2d, rect.Height / 2d);
            var bottomRight = ClampRadius(CornerRadius.BottomRight, rect.Width / 2d, rect.Height / 2d);
            var geometry = CreateGeometry(rect, topRadius, bottomLeft, bottomRight);
            var pen = BorderBrush == null || strokeThickness <= 0d ? null : new Pen(BorderBrush, strokeThickness);

            dc.DrawGeometry(Background, pen, geometry);
        }

        private static double ClampRadius(double radius, double maxWidth, double maxHeight)
        {
            if (radius <= 0d)
            {
                return 0d;
            }

            return Math.Min(radius, Math.Min(maxWidth, maxHeight));
        }

        private static StreamGeometry CreateGeometry(Rect rect, double topRadius, double bottomLeftRadius, double bottomRightRadius)
        {
            var geometry = new StreamGeometry();

            using (var ctx = geometry.Open())
            {
                ctx.BeginFigure(new Point(rect.Left, rect.Top), true, true);

                if (topRadius > 0d)
                {
                    ctx.ArcTo(new Point(rect.Left + topRadius, rect.Top+topRadius), new Size(topRadius, topRadius), 0d, false,
                        SweepDirection.Clockwise, true, false);
                }
                else
                {
                    ctx.LineTo(new Point(rect.Left, rect.Top), true, false);
                }

                ctx.LineTo(new Point(rect.Left + topRadius,rect.Bottom - bottomLeftRadius), true, false);

                if (bottomLeftRadius > 0d)
                {
                    ctx.ArcTo(new Point(rect.Left + topRadius + bottomLeftRadius, rect.Bottom), new Size(bottomLeftRadius, bottomLeftRadius),
                        0d, false, SweepDirection.Counterclockwise, true, false);
                }
                else
                {
                    ctx.LineTo(new Point(rect.Left, rect.Bottom), true, false);
                }

                ctx.LineTo(new Point(rect.Right-topRadius-bottomLeftRadius, rect.Bottom), true, false);

                if (bottomRightRadius > 0d)
                {
                    ctx.ArcTo(new Point(rect.Right - topRadius, rect.Bottom - bottomRightRadius), new Size(bottomRightRadius, bottomRightRadius),
                        0d, false, SweepDirection.Counterclockwise, true, false);
                }
                else
                {
                    ctx.LineTo(new Point(rect.Right, rect.Bottom), true, false);
                }

                ctx.LineTo(new Point(rect.Right - topRadius, rect.Top + topRadius), true, false);

                if (topRadius > 0d)
                {
                    ctx.ArcTo(new Point(rect.Right, rect.Top), new Size(topRadius, topRadius), 0d, false,
                        SweepDirection.Clockwise, true, false);
                }
                else
                {
                    ctx.LineTo(new Point(rect.Right, rect.Top), true, false);
                }

                ctx.LineTo(new Point(rect.Left, rect.Top), true, false);
                ctx.Close();
            }

            geometry.Freeze();
            return geometry;
        }
    }
}
