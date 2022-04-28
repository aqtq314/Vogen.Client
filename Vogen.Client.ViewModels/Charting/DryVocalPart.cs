using Doaz.Reactive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vogen.Synth;

namespace Vogen.Client.ViewModels.Charting
{
    public class DryVocalPart : PartBase
    {
        /* 波形
         * 频谱
         * 歌词符号
         * 歌词拼音
         * 歌词音素
         * 音符音高
         * 音符衔接
         * 音高曲线
         * 假声
         * 张力
         */

        float[] _InAudio;
        float[] _F0;

        public float[] InAudio
        {
            get => _InAudio;
            set => SetAndNotify(ref _InAudio, value);
        }

        public float[] F0
        {
            get => _F0;
            set => SetAndNotify(ref _F0, value);
        }

        public AudioGraphemeChart GraphemeChart { get; init; }
        public AudioPhonemeChart PhonemeChart { get; init; }

        public DryVocalPart(MidiClock offset, float[] inAudio, float[]? outAudio = null,
            float[]? f0 = null,
            IEnumerable<AudioGraphemeItem>? graphemes = null,
            IEnumerable<TimedValueItem<TimeSpan, string>>? phonemes = null)
            : base(offset, outAudio)
        {
            _InAudio = inAudio;
            _F0 = f0 ?? Array.Empty<float>();

            GraphemeChart = new AudioGraphemeChart(graphemes);
            PhonemeChart = new AudioPhonemeChart(phonemes);
        }

        public void FillF0()
        {
            var x = new double[InAudio.Length];
            for (int i = 0; i < x.Length; i++)
                x[i] = InAudio[i];

            var (f0, t) = World.Dio(x, Params.fs, f0Floor: 60, f0Ceil: 1400, framePeriod: Params.hopSize.TotalMilliseconds);
            f0 = World.Stonemask(x, Params.fs, t, f0);

            var f0f = new float[f0.Length];
            for (int i = 0; i < f0.Length; i++)
                f0f[i] = (float)f0[i];
            F0 = f0f;
        }
    }
}
