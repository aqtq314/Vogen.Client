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
        public TimeSignatureChart TimeSig { get; init; }
        public TempoChart Tempo { get; init; }

        public ObservableCollection<Track> Tracks { get; init; }

        public Composition(
            TimeSignature? initTimeSig = null,
            double initTempo = 120,
            IEnumerable<TimedValueItem<int, TimeSignature>>? timeSigs = null,
            IEnumerable<TimedValueItem<MidiClock, double>>? tempos = null,
            IEnumerable<Track>? tracks = null)
        {
            TimeSig = new TimeSignatureChart(
                initTimeSig ?? new TimeSignature(4, 4),
                timeSigs ?? Enumerable.Empty<TimedValueItem<int, TimeSignature>>());
            Tempo = new TempoChart(
                initTempo,
                tempos ?? Enumerable.Empty<TimedValueItem<MidiClock, double>>());

            Tracks = new ObservableCollection<Track>(tracks ?? Enumerable.Empty<Track>());
        }
    }
}
