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

    public class NoteItem : MidiEventItem
    {
        public bool IsRest
        {
            get => (bool)GetValue(IsRestProperty);
            set => SetValue(IsRestProperty, value);
        }
        public static DependencyProperty IsRestProperty { get; } =
            DependencyProperty.Register(nameof(IsRest), typeof(bool), typeof(NoteItem),
                new FrameworkPropertyMetadata(false));

        public double Pitch
        {
            get => (double)GetValue(PitchProperty);
            set => SetValue(PitchProperty, value);
        }
        public static DependencyProperty PitchProperty { get; } =
            DependencyProperty.Register(nameof(Pitch), typeof(double), typeof(NoteItem),
                new FrameworkPropertyMetadata(0.0,
                    FrameworkPropertyMetadataOptions.AffectsParentMeasure | FrameworkPropertyMetadataOptions.AffectsParentArrange));

        public double InternalDeltaPitch
        {
            get => (double)GetValue(InternalDeltaPitchProperty);
            set => SetValue(InternalDeltaPitchProperty, value);
        }
        public static DependencyProperty InternalDeltaPitchProperty { get; } =
            DependencyProperty.Register(nameof(InternalDeltaPitch), typeof(double), typeof(NoteItem),
                new FrameworkPropertyMetadata(0.0));

        //public NoteItemErrorType ErrorType
        //{
        //    get => (NoteItemErrorType)GetValue(ErrorTypeProperty);
        //    set => SetValue(ErrorTypeProperty, value);
        //}
        //public static DependencyProperty ErrorTypeProperty { get; } =
        //    DependencyProperty.Register(nameof(ErrorType), typeof(NoteItemErrorType), typeof(NoteItem),
        //        new FrameworkPropertyMetadata(NoteItemErrorType.None));

        public NoteItem()
        {
        }
    }
}
