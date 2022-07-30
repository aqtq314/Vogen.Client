using Doaz.Reactive;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Vogen.Client.Converters;
using Vogen.Client.Utils;
using Vogen.Client.ViewModels.Charting;

namespace Vogen.Client.Controls
{
    public class MidiClockRuler : FrameworkElement
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

        public static IEnumerable<MidiClock> ListTickHops(TimeSignature timeSig)
        {
            yield return new MidiClock(1);
            yield return new MidiClock(5);

            for (long length = 15; length < timeSig.TicksPerBeat; length <<= 1)
                yield return new MidiClock(length);

            yield return new MidiClock(timeSig.TicksPerBeat);

            long minMultiBeatHop = timeSig.TicksPerMeasure;
            for (long length = minMultiBeatHop >> 1; length > timeSig.TicksPerBeat && length % timeSig.TicksPerBeat == 0; length >>= 1)
                minMultiBeatHop = length;
            for (long length = minMultiBeatHop; length < timeSig.TicksPerMeasure; length <<= 1)
                yield return new MidiClock(length);

            for (long length = timeSig.TicksPerMeasure; ; length <<= 1)
                yield return new MidiClock(length);
        }

        public static MidiClock FindTickHop(TimeSignature timeSig, double quarterWidth, double minTickHop) =>
            ListTickHops(timeSig).First(hop => ChartUnitConversion.MidiClockToPixel(quarterWidth, 0, hop) >= minTickHop);

        public TimeSignatureChart TimeSignatureChart
        {
            get => (TimeSignatureChart)GetValue(TimeSignatureChartProperty);
            set => SetValue(TimeSignatureChartProperty, value);
        }

        public static DependencyProperty TimeSignatureChartProperty { get; } =
            DependencyProperty.Register(nameof(TimeSignatureChart), typeof(TimeSignatureChart), typeof(MidiClockRuler),
                new FrameworkPropertyMetadata(new TimeSignatureChart(new TimeSignature(4, 4)),
                    FrameworkPropertyMetadataOptions.AffectsRender));

        protected override void OnRender(DrawingContext dc)
        {
            var actualWidth = ActualWidth;
            var actualHeight = ActualHeight;
            var quarterWidth = MidiCharting.GetQuarterWidth(this);
            var hOffset = MidiCharting.GetHOffset(this);

            var minPulse = MidiClock.FloorFrom(ChartUnitConversion.PixelToMidiClock(quarterWidth, hOffset, 0));
            var maxPulse = MidiClock.CeilFrom(ChartUnitConversion.PixelToMidiClock(quarterWidth, hOffset, actualWidth));

            using var _ = dc.UsingClip(new RectangleGeometry(new Rect(new Size(actualWidth, actualHeight))));

            // background
            dc.DrawRectangle(Brushes.Transparent, null, new Rect(new Size(actualWidth, actualHeight)));

            // bottom border
            dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, actualHeight - 0.5, actualWidth, 0.5));

            // tickmarks
            for (int i = 0; i <= TimeSignatureChart.Count; i++)
            {
                var maxPulseInTimeSig =
                        i < TimeSignatureChart.Count ?
                        TimeSignatureChart.TimeCodeToMidiTime(TimeSignatureChart[i].Time, 0, 0) :
                        maxPulse;
                maxPulseInTimeSig = maxPulse < maxPulseInTimeSig ? maxPulse : maxPulseInTimeSig;
                if (maxPulseInTimeSig < minPulse)
                    continue;

                var timeSig = i > 0 ? TimeSignatureChart[i - 1].Value : TimeSignatureChart.InitialValue;
                var minorHop = FindTickHop(timeSig, quarterWidth, MinMinorTickHop);
                var majorHop = FindTickHop(timeSig, quarterWidth, MinMajorTickHop);
                var currPulse = TimeSignatureChart.TimeCodeToMidiTime(
                        i == 0 ? 0 : TimeSignatureChart[i - 1].Time, 0, 0);
                if (minPulse > currPulse) 
                    currPulse = (minPulse - currPulse).Tick / minorHop.Tick * minorHop + currPulse;

                for (; currPulse < maxPulseInTimeSig; currPulse += minorHop)
                {
                    var (measure, beat, ticksInBeat) = TimeSignatureChart.MidiTimeToTimeCode(currPulse);
                    var isMajor = (currPulse - TimeSignatureChart.TimeCodeToMidiTime(
                        i == 0 ? 0 : TimeSignatureChart[i - 1].Time, 0, 0)).Tick % majorHop.Tick == 0;
                    var xPos = ChartUnitConversion.PulseToPixel(quarterWidth, hOffset, currPulse.Tick);
                    var height = isMajor ? majorTickHeight : minorTickHeight;
                    dc.DrawLine(tickPen, new Point(xPos, actualHeight - height), new Point(xPos, actualHeight));

                    if (isMajor)
                    {
                        var textStr =
                            majorHop.Tick % timeSig.TicksPerMeasure == 0 ? $"{measure + 1}" :
                            majorHop.Tick % timeSig.TicksPerBeat == 0 ? $"{measure + 1}:{beat + 1}" :
                            $"{measure + 1}:{beat + 1}.{ticksInBeat}";
                        var ft = this.MakeFormattedText(textStr);
                        var halfTextWidth = ft.Width / 2;
                        if (xPos - halfTextWidth >= 0.0 && xPos + halfTextWidth <= actualWidth)
                            dc.DrawText(ft, new Point(xPos - halfTextWidth, actualHeight - ft.Height - majorTickHeight));
                    }
                }
                if (maxPulseInTimeSig >= maxPulse)
                    break;
            }
        }
    }
}