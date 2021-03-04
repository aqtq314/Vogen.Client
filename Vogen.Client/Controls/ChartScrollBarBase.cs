using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Vogen.Client.Controls
{
    public abstract class ChartScrollBarBase : Control
    {
        public static readonly DependencyProperty ScrollValueProperty =
            DependencyProperty.Register(nameof(ScrollValue), typeof(double), typeof(ChartScrollBarBase),
                new FrameworkPropertyMetadata(0.0));

        public static readonly DependencyProperty ViewportSizeProperty =
            DependencyProperty.Register(nameof(ViewportSize), typeof(double), typeof(ChartScrollBarBase),
                new FrameworkPropertyMetadata(0.0));

        public double ScrollValue
        {
            get => (double)GetValue(ScrollValueProperty);
            set => SetValue(ScrollValueProperty, value);
        }

        public double ViewportSize
        {
            get => (double)GetValue(ViewportSizeProperty);
            set => SetValue(ViewportSizeProperty, value);
        }
    }
}
