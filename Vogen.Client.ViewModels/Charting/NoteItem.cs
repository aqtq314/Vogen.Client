using Doaz.Reactive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vogen.Client.ViewModels.Charting
{
    public class NoteItem : TimedEventItem<MidiClock>
    {
        public const string RestLyric = "";
        public const string SlurLyric = "-";
        public const string ContLyric = "+";

        string _Lyric;
        string _Rom;
        double _Pitch;

        public bool IsRest => Lyric == RestLyric;
        public bool IsSlur => Lyric == SlurLyric;
        public bool IsCont => Lyric == ContLyric;

        public string Lyric
        {
            get => _Lyric;
            set
            {
                SetAndNotify(ref _Lyric, value);
                OnPropertyChanged(nameof(IsRest));
                OnPropertyChanged(nameof(IsSlur));
                OnPropertyChanged(nameof(IsCont));
            }
        }

        public string Rom
        {
            get => _Rom;
            set => SetAndNotify(ref _Rom, value);
        }

        public double Pitch
        {
            get => _Pitch;
            set => SetAndNotify(ref _Pitch, value);
        }

        public NoteItem(MidiClock time, string lyric, string rom, double pitch)
            : base(time)
        {
            _Lyric = lyric;
            _Rom = rom;
            _Pitch = pitch;
        }

        public static NoteItem CreateRest(MidiClock time) => new NoteItem(time, RestLyric, "", 0);
        public static NoteItem CreateSlur(MidiClock time, double pitch) => new NoteItem(time, SlurLyric, "", pitch);
        public static NoteItem CreateCont(MidiClock time, double pitch) => new NoteItem(time, ContLyric, "", pitch);
    }
}
