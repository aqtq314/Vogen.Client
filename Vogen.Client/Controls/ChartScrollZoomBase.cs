using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Vogen.Client.Controls
{
    public class ChartScrollZoomBase : Control
    {
        static TimeSpan animationDuration = TimeSpan.FromSeconds(0.1);

        private void AnimateDependencyProperty(double newValue, DependencyProperty dp)
        {
            if (EnableAnimation)
                BeginAnimation(dp,
                    new DoubleAnimation(newValue, new Duration(animationDuration))
                    { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } });

            else
                BeginAnimation(dp,
                    new DoubleAnimation(newValue, new Duration(TimeSpan.Zero)));
        }

        public bool EnableAnimation
        {
            get => (bool)GetValue(EnableAnimationProperty);
            set => SetValue(EnableAnimationProperty, value);
        }
        public static DependencyProperty EnableAnimationProperty { get; } =
            DependencyProperty.Register(nameof(EnableAnimation), typeof(bool), typeof(ChartScrollZoomBase),
                new FrameworkPropertyMetadata(true));

        public double ScrollMinimum
        {
            get => (double)GetValue(ScrollMinimumProperty);
            set => SetValue(ScrollMinimumProperty, value);
        }
        private void OnScrollMinimumChanged(double oldValue, double newValue) { CoerceValue(ScrollValueProperty); }
        public static DependencyProperty ScrollMinimumProperty { get; } =
            DependencyProperty.Register(nameof(ScrollMinimum), typeof(double), typeof(ChartScrollZoomBase),
                new FrameworkPropertyMetadata(0.0,
                    (d, e) => ((ChartScrollZoomBase)d).OnScrollMinimumChanged((double)e.OldValue, (double)e.NewValue)));

        public double ScrollMaximum
        {
            get => (double)GetValue(ScrollMaximumProperty);
            set => SetValue(ScrollMaximumProperty, value);
        }
        private void OnScrollMaximumChanged(double oldValue, double newValue) { CoerceValue(ScrollValueProperty); }
        public static DependencyProperty ScrollMaximumProperty { get; } =
            DependencyProperty.Register(nameof(ScrollMaximum), typeof(double), typeof(ChartScrollZoomBase),
                new FrameworkPropertyMetadata(1.0,
                    (d, e) => ((ChartScrollZoomBase)d).OnScrollMaximumChanged((double)e.OldValue, (double)e.NewValue)));

        public double ScrollViewport
        {
            get => (double)GetValue(ScrollViewportProperty);
            set => SetValue(ScrollViewportProperty, value);
        }
        public static DependencyProperty ScrollViewportProperty { get; } =
            DependencyProperty.Register(nameof(ScrollViewport), typeof(double), typeof(ChartScrollZoomBase),
                new FrameworkPropertyMetadata(0.0));

        public double ScrollValue
        {
            get => (double)GetValue(ScrollValueProperty);
            set => SetValue(ScrollValueProperty, value);
        }
        private void OnScrollValueChanged(double oldValue, double newValue)
        {
            AnimateDependencyProperty(CoerceScrollValue(newValue), ScrollValueAnimatedProperty);
        }
        public double CoerceScrollValue(double _ScrollValue) => Math.Max(ScrollMinimum, Math.Min(ScrollMaximum, _ScrollValue));
        public static DependencyProperty ScrollValueProperty { get; } =
            DependencyProperty.Register(nameof(ScrollValue), typeof(double), typeof(ChartScrollZoomBase),
                new FrameworkPropertyMetadata(0.0,
                    (d, e) => ((ChartScrollZoomBase)d).OnScrollValueChanged((double)e.OldValue, (double)e.NewValue)));

        public double ScrollValueAnimated
        {
            get => (double)GetValue(ScrollValueAnimatedProperty);
            set => SetValue(ScrollValueAnimatedProperty, value);
        }
        public static DependencyProperty ScrollValueAnimatedProperty { get; } =
            DependencyProperty.Register(nameof(ScrollValueAnimated), typeof(double), typeof(ChartScrollZoomBase),
                new FrameworkPropertyMetadata(0.0));

        public double Log2ZoomMinimum
        {
            get => (double)GetValue(Log2ZoomMinimumProperty);
            set => SetValue(Log2ZoomMinimumProperty, value);
        }
        private void OnLog2ZoomMinimumChanged(double oldValue, double newValue) { CoerceValue(Log2ZoomValueProperty); }
        public static DependencyProperty Log2ZoomMinimumProperty { get; } =
            DependencyProperty.Register(nameof(Log2ZoomMinimum), typeof(double), typeof(ChartScrollZoomBase),
                new FrameworkPropertyMetadata(0.0,
                    (d, e) => ((ChartScrollZoomBase)d).OnLog2ZoomMinimumChanged((double)e.OldValue, (double)e.NewValue)));

        public double Log2ZoomMaximum
        {
            get => (double)GetValue(Log2ZoomMaximumProperty);
            set => SetValue(Log2ZoomMaximumProperty, value);
        }
        private void OnLog2ZoomMaximumChanged(double oldValue, double newValue) { CoerceValue(Log2ZoomValueProperty); }
        public static DependencyProperty Log2ZoomMaximumProperty { get; } =
            DependencyProperty.Register(nameof(Log2ZoomMaximum), typeof(double), typeof(ChartScrollZoomBase),
                new FrameworkPropertyMetadata(1.0,
                    (d, e) => ((ChartScrollZoomBase)d).OnLog2ZoomMaximumChanged((double)e.OldValue, (double)e.NewValue)));

        public double Log2ZoomValue
        {
            get => (double)GetValue(Log2ZoomValueProperty);
            set => SetValue(Log2ZoomValueProperty, value);
        }
        private void OnLog2ZoomValueChanged(double oldValue, double newValue)
        {
            AnimateDependencyProperty(CoerceLog2ZoomValue(newValue), Log2ZoomValueAnimatedProperty);
        }
        public double CoerceLog2ZoomValue(double _Log2ZoomValue) => Math.Max(Log2ZoomMinimum, Math.Min(Log2ZoomMaximum, _Log2ZoomValue));
        public static DependencyProperty Log2ZoomValueProperty { get; } =
            DependencyProperty.Register(nameof(Log2ZoomValue), typeof(double), typeof(ChartScrollZoomBase),
                new FrameworkPropertyMetadata(0.0,
                    (d, e) => ((ChartScrollZoomBase)d).OnLog2ZoomValueChanged((double)e.OldValue, (double)e.NewValue)));

        public double Log2ZoomValueAnimated
        {
            get => (double)GetValue(Log2ZoomValueAnimatedProperty);
            set => SetValue(Log2ZoomValueAnimatedProperty, value);
        }
        public static DependencyProperty Log2ZoomValueAnimatedProperty { get; } =
            DependencyProperty.Register(nameof(Log2ZoomValueAnimated), typeof(double), typeof(ChartScrollZoomBase),
                new FrameworkPropertyMetadata(0.0));
    }
}
