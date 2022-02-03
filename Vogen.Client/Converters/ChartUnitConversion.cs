using Doaz.Reactive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vogen.Client.Converters
{
    public static class ChartUnitConversion
    {
        private static long CeilDiv(long dividend, long divisor)
        {
            var ratio = dividend / divisor;
            return (dividend ^ divisor) >= 0 && (dividend % divisor) != 0 ? ratio + 1 : ratio;
        }

        public static double PulseToPixel(double quarterWidth, double hOffset, double pulses) =>
            (pulses - hOffset) / Midi.ppqn * quarterWidth;

        public static double PixelToPulse(double quarterWidth, double hOffset, double xPos) =>
            hOffset + xPos * Midi.ppqn / quarterWidth;

        public static double PitchToPixel(double keyHeight, double actualHeight, double vOffset, double pitch) =>
            actualHeight / 2 - (pitch - vOffset) * keyHeight;

        public static double PixelToPitch(double keyHeight, double actualHeight, double vOffset, double yPos) =>
            vOffset + (actualHeight / 2 - yPos) / keyHeight;

        public static long Quantize(bool snap, long quantization, TimeSignature timeSig, long pulses)
        {
            if (!snap) return pulses;
            else
            {
                var pulsesMeasureQuantized = pulses / timeSig.PulsesPerMeasure * timeSig.PulsesPerMeasure;
                return pulsesMeasureQuantized + (pulses - pulsesMeasureQuantized) / quantization * quantization;
            }
        }

        public static long QuantizeCeil(bool snap, long quantization, TimeSignature timeSig, long pulses)
        {
            if (!snap) return pulses;
            else
            {
                var pulsesMeasureQuantized = pulses / timeSig.PulsesPerMeasure * timeSig.PulsesPerMeasure;
                return pulsesMeasureQuantized + Math.Min(timeSig.PulsesPerMeasure, CeilDiv((pulses - pulsesMeasureQuantized), quantization) * quantization);
            }
        }
    }
}
