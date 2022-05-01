using Doaz.Reactive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vogen.Client.Utils;

namespace Vogen.Client.Converters
{
    public static class ChartUnitConversion
    {
        public static double PulseToPixel(double quarterWidth, double hOffset, double pulses) =>
            (pulses - hOffset) / Midi.ppqn * quarterWidth;

        public static double PixelToPulse(double quarterWidth, double hOffset, double xPos) =>
            hOffset + xPos * Midi.ppqn / quarterWidth;

        public static double MidiClockToPixel(double quarterWidth, double hOffset, MidiClockF midiClock) =>
            (midiClock.Tick - hOffset) / Midi.ppqn * quarterWidth;

        public static double MidiClockToPixel(double quarterWidth, double hOffset, MidiClock midiClock) =>
            MidiClockToPixel(quarterWidth, hOffset, (MidiClockF)midiClock);

        public static MidiClockF PixelToMidiClock(double quarterWidth, double hOffset, double xPos) =>
            new MidiClockF(hOffset + xPos * Midi.ppqn / quarterWidth);

        public static double PitchToPixel(double keyHeight, double actualHeight, double vOffset, double pitch) =>
            actualHeight / 2 - (pitch - vOffset) * keyHeight;

        public static double PixelToPitch(double keyHeight, double actualHeight, double vOffset, double yPos) =>
            vOffset + (actualHeight / 2 - yPos) / keyHeight;

        //public static MidiClock Quantize(bool snap, long quantization, TimeSignature timeSig, MidiClock pulses)
        //{
        //    if (!snap) return pulses;
        //    else
        //    {
        //        var pulsesMeasureQuantized = pulses.Tick / timeSig.TicksPerMeasure * timeSig.TicksPerMeasure;
        //        return new MidiClock(pulsesMeasureQuantized + (pulses.Tick - pulsesMeasureQuantized) / quantization * quantization);
        //    }
        //}

        //public static MidiClock QuantizeCeil(bool snap, long quantization, TimeSignature timeSig, MidiClock pulses)
        //{
        //    if (!snap) return pulses;
        //    else
        //    {
        //        var pulsesMeasureQuantized = pulses.Tick / timeSig.TicksPerMeasure * timeSig.TicksPerMeasure;
        //        return new MidiClock(pulsesMeasureQuantized + Math.Min(timeSig.TicksPerMeasure, (pulses.Tick - pulsesMeasureQuantized).CeilDiv(quantization) * quantization));
        //    }
        //}
    }
}
