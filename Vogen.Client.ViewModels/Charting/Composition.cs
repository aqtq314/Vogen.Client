using Doaz.Reactive;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vogen.Client.ViewModels.Charting
{
    public class Composition : ViewModelBase
    {
        public TimeSignatureChart TimeSigs { get; init; }
        public TempoChart Tempos { get; init; }
        public TrackList Tracks { get; init; }
        public PartChart Parts { get; init; }

        public Composition(
            TimeSignature? initTimeSig = null,
            double initTempo = 120,
            IEnumerable<TimedValueItem<int, TimeSignature>>? timeSigs = null,
            IEnumerable<TimedValueItem<MidiClock, double>>? tempos = null,
            IEnumerable<Track>? tracks = null,
            IEnumerable<PartBase>? parts = null)
        {
            TimeSigs = new TimeSignatureChart(
                initTimeSig ?? new TimeSignature(4, 4),
                timeSigs ?? Enumerable.Empty<TimedValueItem<int, TimeSignature>>());
            Tempos = new TempoChart(
                initTempo,
                tempos ?? Enumerable.Empty<TimedValueItem<MidiClock, double>>());

            Tracks = new TrackList(tracks ?? Enumerable.Empty<Track>());
            Parts = new PartChart(parts ?? Enumerable.Empty<PartBase>());
        }
    }
}
