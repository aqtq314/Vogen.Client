using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vogen.Client.ViewModels.Charting
{
    public class AudioGraphemeItem : TimedEventItem<TimeSpan>
    {
        public const string RestLyric = "";
        public const string BreathLyric = "<br>";

        string _Lyric;
        string _Rom;

        public bool IsRest => Lyric == RestLyric;
        public bool IsBreath => Lyric == BreathLyric;

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

        public AudioGraphemeItem(TimeSpan time, string lyric, string rom)
            : base(time)
        {
            _Lyric = lyric;
            _Rom = rom;
        }

        public static AudioGraphemeItem CreateRest(TimeSpan time) => new AudioGraphemeItem(time, RestLyric, "");
        public static AudioGraphemeItem CreateBreath(TimeSpan time) => new AudioGraphemeItem(time, BreathLyric, "");
    }
}
