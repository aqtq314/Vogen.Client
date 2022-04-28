using Doaz.Reactive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vogen.Client.ViewModels.Charting
{
    public class F0AnchorItem : TimedEventItem<MidiClock>
    {
        double _Pitch;
        bool _IsDisconnected;

        public double Pitch
        {
            get => _Pitch;
            set => SetAndNotify(ref _Pitch, value);
        }

        public bool IsDisconnected
        {
            get => _IsDisconnected;
            set => SetAndNotify(ref _IsDisconnected, value);
        }

        public F0AnchorItem(MidiClock time, double pitch, bool isDisconnected = false)
            : base(time)
        {
            _Pitch = pitch;
            _IsDisconnected = isDisconnected;
        }
    }
}
