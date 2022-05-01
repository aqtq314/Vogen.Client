using Doaz.Reactive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vogen.Client.ViewModels.Charting
{
    public abstract class PartBase : TimedEventItem<MidiClock>
    {
        Track _Track;
        float[] _OutAudio;

        public Track Track
        {
            get => _Track;
            set => SetAndNotify(ref _Track, value);
        }

        public float[] OutAudio
        {
            get => _OutAudio;
            set => SetAndNotify(ref _OutAudio, value);
        }

        protected PartBase(MidiClock time, Track track, float[]? outAudio = null)
            : base(time)
        {
            _Track = track;
            _OutAudio = outAudio ?? Array.Empty<float>();
        }
    }
}
