using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vogen.Client.ViewModels
{
    public class Note : ViewModelBase
    {
        public const string HyphenLyric = "-";
        public const double RestPitch = -1;

        double _Pitch;
        string _Lyric;
        string _Rom;
        long _On;

        public double Pitch
        {
            get => _Pitch;
            set => SetAndNotify(ref _Pitch, value);
        }

        public string Lyric
        {
            get => _Lyric;
            set => SetAndNotify(ref _Lyric, value);
        }

        public string Rom
        {
            get => _Rom;
            set => SetAndNotify(ref _Rom, value);
        }

        public long On
        {
            get => _On;
            set => SetAndNotify(ref _On, value);
        }

        public bool GetIsHyphen() => Lyric == HyphenLyric;
        public bool GetIsRest() => Pitch == RestPitch;

        public Note(double pitch, string lyric, string rom, long on)
        {
            _Pitch = pitch;
            _Lyric = lyric;
            _Rom = rom;
            _On = on;
        }

        public Note(double pitch, long on) : this(pitch, HyphenLyric, "", on) { }

        public Note(long on) : this(RestPitch, "", "", on) { }

        public static int CompareByOnset(Note n1, Note n2)
        {
            return Comparer<long>.Default.Compare(n1.On, n2.On);
        }
    }
}
