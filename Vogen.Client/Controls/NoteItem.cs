using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Vogen.Client.ViewModels;

namespace Vogen.Client.Controls
{
    public enum NoteItemErrorType
    {
        None = 0,
        NoStartingNote,
        NoEndingRest,
        ConsecutiveRests,
        ZeroDuration,
    }

    public class NoteItem : ContentControl
    {
        private static int instanceCount = 0;

        public long Onset
        {
            get => (long)GetValue(OnsetProperty);
            set => SetValue(OnsetProperty, value);
        }
        public static DependencyProperty OnsetProperty { get; } =
            DependencyProperty.Register(nameof(Onset), typeof(long), typeof(NoteItem),
                new FrameworkPropertyMetadata(0L,
                    FrameworkPropertyMetadataOptions.AffectsParentMeasure | FrameworkPropertyMetadataOptions.AffectsParentArrange));

        public double Pitch
        {
            get => (double)GetValue(PitchProperty);
            set => SetValue(PitchProperty, value);
        }
        public static DependencyProperty PitchProperty { get; } =
            DependencyProperty.Register(nameof(Pitch), typeof(double), typeof(NoteItem),
                new FrameworkPropertyMetadata(Note.RestPitch,
                    FrameworkPropertyMetadataOptions.AffectsParentMeasure | FrameworkPropertyMetadataOptions.AffectsParentArrange));

        public double InternalDeltaPitch
        {
            get => (double)GetValue(InternalDeltaPitchProperty);
            set => SetValue(InternalDeltaPitchProperty, value);
        }
        public static DependencyProperty InternalDeltaPitchProperty { get; } =
            DependencyProperty.Register(nameof(InternalDeltaPitch), typeof(double), typeof(NoteItem),
                new FrameworkPropertyMetadata(0.0));

        public NoteItemErrorType ErrorType
        {
            get => (NoteItemErrorType)GetValue(ErrorTypeProperty);
            set => SetValue(ErrorTypeProperty, value);
        }
        public static DependencyProperty ErrorTypeProperty { get; } =
            DependencyProperty.Register(nameof(ErrorType), typeof(NoteItemErrorType), typeof(NoteItem),
                new FrameworkPropertyMetadata(NoteItemErrorType.None));

        public NoteItem()
        {
        }
    }
}
