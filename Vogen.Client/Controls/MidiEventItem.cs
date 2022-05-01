using Doaz.Reactive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Vogen.Client.Controls
{
    public class MidiEventItem : ContentControl
    {
        public MidiClock Onset
        {
            get => (MidiClock)GetValue(OnsetProperty);
            set => SetValue(OnsetProperty, value);
        }

        public bool HasLeftOverflow
        {
            get => (bool)GetValue(HasLeftOverflowProperty);
            set => SetValue(HasLeftOverflowProperty, value);
        }

        public bool HasRightOverflow
        {
            get => (bool)GetValue(HasRightOverflowProperty);
            set => SetValue(HasRightOverflowProperty, value);
        }

        public static DependencyProperty OnsetProperty { get; } =
            DependencyProperty.Register(nameof(Onset), typeof(MidiClock), typeof(MidiEventItem),
                new FrameworkPropertyMetadata(MidiClock.Zero,
                    FrameworkPropertyMetadataOptions.AffectsParentMeasure | FrameworkPropertyMetadataOptions.AffectsParentArrange));

        public static DependencyProperty HasLeftOverflowProperty { get; } =
            DependencyProperty.Register(nameof(HasLeftOverflow), typeof(bool), typeof(MidiEventItem),
                new FrameworkPropertyMetadata(false));

        public static DependencyProperty HasRightOverflowProperty { get; } =
            DependencyProperty.Register(nameof(HasRightOverflow), typeof(bool), typeof(MidiEventItem),
                new FrameworkPropertyMetadata(false));
    }
}
