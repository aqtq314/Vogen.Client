using Doaz.Reactive;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vogen.Client.ViewModels.Charting
{
    public class NotePart : PartBase
    {
        /* 歌词符号 (故事; 仕事; rememberance)
         * 歌词拼音 (gu, shi; shi, go, to; rə, mɛm, bɹəns)
         * 歌词音素 (g, u, ʂ, ʐ̩; ɕ, i, ɡ, o̞, t, o̞; ɹ, ə, m, ɛ, m̯, b, ɹ, ə, n̯, s)
         * 音符音高 (67, 69.1, 69.2, 74.1)
         * 音高曲线 (Bezier Interpolation)
         * 换气音量 (Bezier Interpolation)
         * 音符假声 (0, 0, 1, 1)
         * 张力 (Bezier Interpolation)
         * 歌手 (Tuo, Rgb, Mei)
         */
        string _Name;
        string _RomScheme;

        public string Name
        {
            get => _Name;
            set => SetAndNotify(ref _Name, value);
        }

        public string RomScheme
        {
            get => _RomScheme;
            set => SetAndNotify(ref _RomScheme, value);
        }

        public NoteChart NoteChart { get; init; }
        public PhonemeChart PhonemeChart { get; init; }
        public F0AnchorChart F0AnchorChart { get; init; }

        public NotePart(MidiClock offset, string name, string romScheme,
            IEnumerable<NoteItem>? notes = null,
            IEnumerable<TimedValueItem<TimeSpan, string?>>? phonemes = null,
            IEnumerable<F0AnchorItem>? f0Anchors = null,
            float[]? outAudio = null)
            : base(offset, outAudio)
        {
            _Name = name;
            _RomScheme = romScheme;

            NoteChart = new NoteChart(notes);
            PhonemeChart = new PhonemeChart(phonemes);
            F0AnchorChart = new F0AnchorChart(f0Anchors);
        }
    }
}
