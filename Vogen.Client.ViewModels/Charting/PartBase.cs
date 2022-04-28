using Doaz.Reactive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vogen.Client.ViewModels.Charting
{
    public abstract class PartBase : ViewModelBase
    {
        MidiClock _Offset;
        float[] _OutAudio;

        public MidiClock Offset
        {
            get => _Offset;
            set => SetAndNotify(ref _Offset, value);
        }

        public float[] OutAudio
        {
            get => _OutAudio;
            set => SetAndNotify(ref _OutAudio, value);
        }

        protected PartBase(MidiClock offset, float[]? outAudio = null)
        {
            _Offset = offset;
            _OutAudio = outAudio ?? Array.Empty<float>();
        }
    }
}
