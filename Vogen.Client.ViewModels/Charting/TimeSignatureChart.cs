using Doaz.Reactive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vogen.Client.ViewModels.Charting
{
    public class TimeSignatureChart : TimedValueChart<int, TimeSignature>
    {
        public TimeSignatureChart(TimeSignature initialValue, IEnumerable<TimedValueItem<int, TimeSignature>>? items = null)
            : base(initialValue, items)
        { }

        public (int measure, int beat, int ticksInBeat) MidiTimeToTimeCode(MidiClock midiTime)
        {
            var prevTimeSig = new TimedValueItem<int, TimeSignature>(0, InitialValue);

            foreach (var timeSig in this)
            {
                var eventInterval = new MidiClock((timeSig.Time - prevTimeSig.Time) * prevTimeSig.Value.TicksPerMeasure);
                if (eventInterval > midiTime) break;

                midiTime -= eventInterval;
                prevTimeSig = timeSig;
            }

            var measure = prevTimeSig.Time + (int)(midiTime.Tick / prevTimeSig.Value.TicksPerMeasure);
            var ticksInMeasure = midiTime.Tick % prevTimeSig.Value.TicksPerMeasure;
            var beat = (int)(ticksInMeasure / prevTimeSig.Value.TicksPerBeat);
            var ticksInBeat = (int)(ticksInMeasure % prevTimeSig.Value.TicksPerBeat);
            return (measure, beat, ticksInBeat);
        }

        public MidiClock TimeCodeToMidiTime(int measure, int beat, int ticksInBeat)
        {
            var prevTimeSig = new TimedValueItem<int, TimeSignature>(0, InitialValue);
            var midiTime = MidiClock.Zero;

            foreach (var timeSig in this)
            {
                if (timeSig.Time >= measure) break;

                midiTime += new MidiClock(timeSig.Time - prevTimeSig.Time) * prevTimeSig.Value.TicksPerMeasure;
                prevTimeSig = timeSig;
            }

            midiTime += new MidiClock(measure - prevTimeSig.Time) * prevTimeSig.Value.TicksPerMeasure;
            midiTime += new MidiClock(beat) * prevTimeSig.Value.TicksPerBeat;
            midiTime += new MidiClock(ticksInBeat);
            return midiTime;
        }
    }
}
