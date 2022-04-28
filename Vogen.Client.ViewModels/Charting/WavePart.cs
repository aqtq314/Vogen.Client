using Doaz.Reactive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vogen.Client.ViewModels.Charting
{
    public class WavePart : PartBase
    {
        string _Name;

        public string Name
        {
            get => _Name;
            set => SetAndNotify(ref _Name, value);
        }

        public WavePart(MidiClock offset, string name, float[]? outAudio = null)
            : base(offset, outAudio)
        {
            _Name = name;
        }
    }
}
