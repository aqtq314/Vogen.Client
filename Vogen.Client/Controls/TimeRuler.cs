using Doaz.Reactive;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Vogen.Client.Converters;
using Vogen.Client.Utils;

namespace Vogen.Client.Controls
{
    public class TimeRuler : FrameworkElement
    {
        static readonly double majorTickHeight = 6;
        static readonly double minorTickHeight = 4;

        static readonly Pen tickPen = new Pen(new SolidColorBrush(ColorConv.rgb(0)), 1.0).Frozen();

        public static ImmutableArray<long> Quantizations { get; } = ImmutableArray.Create<long>(
            1920, 960, 480, 240, 120, 60, 30, 15, 1,
            320, 160, 80, 40, 20);
        public static ImmutableArray<long> QuantizationsSorted { get; } = Quantizations.Sort();

        public static double MinMajorTickHop { get; } = 70;    // in screen pixels
        public static double MinMinorTickHop { get; } = 25;

        public static IEnumerable<long> ListTickHops(TimeSignature timeSig)
        {
            yield return 1;
            yield return 5;

            for (long length = 15; length < timeSig.PulsesPerBeat; length <<= 1)
                yield return length;

            yield return timeSig.PulsesPerBeat;

            long minMultiBeatHop = timeSig.PulsesPerMeasure;
            for (long length = minMultiBeatHop >> 1; length > timeSig.PulsesPerBeat && length % timeSig.PulsesPerBeat == 0; length >>= 1)
                minMultiBeatHop = length;
            for (long length = minMultiBeatHop; length < timeSig.PulsesPerMeasure; length <<= 1)
                yield return length;

            for (long length = timeSig.PulsesPerMeasure; ; length <<= 1)
                yield return length;
        }

        public static long FindTickHop(TimeSignature timeSig, double quarterWidth, double minTickHop) =>
            ListTickHops(timeSig).First(hop => ChartUnitConversion.PulseToPixel(quarterWidth, 0, hop) >= minTickHop);

        public TimeSignature TimeSignature
        {
            get => (TimeSignature)GetValue(TimeSignatureProperty);
            set => SetValue(TimeSignatureProperty, value);
        }
        public static DependencyProperty TimeSignatureProperty { get; } =
            DependencyProperty.Register(nameof(TimeSignature), typeof(TimeSignature), typeof(TimeRuler),
                new FrameworkPropertyMetadata(new TimeSignature(4, 4), FrameworkPropertyMetadataOptions.AffectsRender));

        protected override void OnRender(DrawingContext dc)
        {
            var actualWidth = ActualWidth;
            var actualHeight = ActualHeight;
            var timeSig = TimeSignature;
            var quarterWidth = NoteChartEditor.GetQuarterWidth(this);
            var hOffset = NoteChartEditor.GetHOffset(this);

            var minPulse = (long)ChartUnitConversion.PixelToPulse(quarterWidth, hOffset, 0);
            var maxPulse = (long)ChartUnitConversion.PixelToPulse(quarterWidth, hOffset, actualWidth);

            var majorHop = FindTickHop(timeSig, quarterWidth, MinMajorTickHop);
            var minorHop = FindTickHop(timeSig, quarterWidth, MinMinorTickHop);

            using var _ = dc.UsingClip(new RectangleGeometry(new Rect(new Size(actualWidth, actualHeight))));

            // background
            dc.DrawRectangle(Brushes.Transparent, null, new Rect(new Size(actualWidth, actualHeight)));

            // bottom border
            dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, actualHeight - 0.5, actualWidth, 0.5));

            // tickmarks
            for (var currPulse = minPulse / minorHop * minorHop; currPulse <= maxPulse; currPulse += minorHop)
            {
                var isMajor = currPulse % majorHop == 0;
                var xPos = ChartUnitConversion.PulseToPixel(quarterWidth, hOffset, currPulse);
                var height = isMajor ? majorTickHeight : minorTickHeight;
                dc.DrawLine(tickPen, new Point(xPos, actualHeight - height), new Point(xPos, actualHeight));

                if (isMajor)
                {
                    var textStr =
                        majorHop % timeSig.PulsesPerMeasure == 0 ? Midi.formatMeasures(timeSig, currPulse) :
                        majorHop % timeSig.PulsesPerBeat == 0 ? Midi.formatMeasureBeats(timeSig, currPulse) :
                        Midi.formatFull(timeSig, currPulse);
                    var ft = this.MakeFormattedText(textStr);
                    var halfTextWidth = ft.Width / 2;
                    if (xPos - halfTextWidth >= 0.0 && xPos + halfTextWidth <= actualWidth)
                        dc.DrawText(ft, new Point(xPos - halfTextWidth, actualHeight - ft.Height - majorTickHeight));
                }
            }
        }
    }
}
