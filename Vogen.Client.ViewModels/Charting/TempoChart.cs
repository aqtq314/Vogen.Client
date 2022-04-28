using Doaz.Reactive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vogen.Client.ViewModels.Charting
{
    public class TempoChart : TimedValueChart<MidiClock, double>
    {
        public TempoChart(double initialValue, IEnumerable<TimedValueItem<MidiClock, double>>? items = null)
            : base(initialValue, items)
        { }

        public TimeSpan MidiTimeToTimeSpan(MidiClock midiTime)
        {
            var prevTempo = new TimedValueItem<MidiClock, double>(MidiClock.Zero, InitialValue);
            var actualTime = TimeSpan.Zero;

            foreach (var tempo in this)
            {
                if (tempo.Time >= midiTime) break;

                actualTime += MidiClock.ToTimeSpan(prevTempo.Value, tempo.Time - prevTempo.Time);
                prevTempo = tempo;
            }

            actualTime += MidiClock.ToTimeSpan(prevTempo.Value, midiTime - prevTempo.Time);
            return actualTime;
        }

        public MidiClockF TimeSpanToMidiTimeF(TimeSpan actualTime)
        {
            var prevTempo = new TimedValueItem<MidiClock, double>(MidiClock.Zero, InitialValue);

            foreach (var tempo in this)
            {
                var eventIntervalTime = MidiClock.ToTimeSpan(prevTempo.Value, tempo.Time - prevTempo.Time);
                if (eventIntervalTime > actualTime) break;

                actualTime -= eventIntervalTime;
                prevTempo = tempo;
            }

            return (MidiClockF)prevTempo.Time + MidiClockF.OfTimeSpan(prevTempo.Value, actualTime);
        }

        public MidiClock TimeSpanToMidiTime(TimeSpan actualTime) =>
            (MidiClock)TimeSpanToMidiTimeF(actualTime);
    }
}
