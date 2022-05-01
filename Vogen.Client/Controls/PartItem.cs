using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Vogen.Client.Controls
{
    public class PartItem : MidiEventItem
    {
        public int TrackIndex
        {
            get => (int)GetValue(TrackIndexProperty);
            set => SetValue(TrackIndexProperty, value);
        }

        public static DependencyProperty TrackIndexProperty { get; } =
            DependencyProperty.Register(nameof(TrackIndex), typeof(int), typeof(PartItem),
                new FrameworkPropertyMetadata(0));
    }
}
