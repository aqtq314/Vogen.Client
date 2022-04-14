using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Vogen.Client.Controls
{
    public class EventItem : ContentControl
    {
        public long Onset
        {
            get => (long)GetValue(OnsetProperty);
            set => SetValue(OnsetProperty, value);
        }
        public static DependencyProperty OnsetProperty { get; } =
            DependencyProperty.Register(nameof(Onset), typeof(long), typeof(EventItem),
                new FrameworkPropertyMetadata(0L,
                    FrameworkPropertyMetadataOptions.AffectsParentMeasure | FrameworkPropertyMetadataOptions.AffectsParentArrange));
    }
}
