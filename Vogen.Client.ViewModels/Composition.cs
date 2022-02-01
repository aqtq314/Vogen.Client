using Doaz.Reactive;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vogen.Client.ViewModels
{
    public class Composition : ViewModelBase
    {
        TimeSignature _InitTimeSig;
        float _InitTempo;

        public TimeSignature InitTimeSig
        {
            get => _InitTimeSig;
            set => SetAndNotify(ref _InitTimeSig, value);
        }

        public float InitTempo
        {
            get => _InitTempo;
            set => SetAndNotify(ref _InitTempo, value);
        }

        public ObservableCollection<NoteTrack> NoteTracks { get; init; }

        public Composition(TimeSignature initTimeSig, float initTempo, IEnumerable<NoteTrack>? noteTracks = null)
        {
            _InitTimeSig = initTimeSig;
            _InitTempo = initTempo;
            NoteTracks = new ObservableCollection<NoteTrack>(noteTracks ?? Enumerable.Empty<NoteTrack>());
        }
    }
}
