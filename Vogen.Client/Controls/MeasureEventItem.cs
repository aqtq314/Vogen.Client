using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Vogen.Client.Controls
{
    public class MeasureEventItem : ContentControl
    {
        public int MeasureIndex
        {
            get => (int)GetValue(MeasureIndexProperty);
            set => SetValue(MeasureIndexProperty, value);
        }
        public static DependencyProperty MeasureIndexProperty { get; } =
            DependencyProperty.Register(nameof(MeasureIndex), typeof(int), typeof(MeasureEventItem),
                new FrameworkPropertyMetadata(0,
                    FrameworkPropertyMetadataOptions.AffectsParentMeasure | FrameworkPropertyMetadataOptions.AffectsParentArrange));
    }
}
