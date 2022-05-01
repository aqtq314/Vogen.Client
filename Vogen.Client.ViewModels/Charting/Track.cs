using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vogen.Client.ViewModels.Charting
{
    public class Track : ViewModelBase
    {
        int _Index;
        string _Name;
        float _Volume;
        bool _IsMuted;

        public int Index
        {
            get => _Index;
            internal set => SetAndNotify(ref _Index, value);
        }

        public string Name
        {
            get => _Name;
            set => SetAndNotify(ref _Name, value);
        }

        public float Volume
        {
            get => _Volume;
            set => SetAndNotify(ref _Volume, value);
        }

        public bool IsMuted
        {
            get => _IsMuted;
            set => SetAndNotify(ref _IsMuted, value);
        }

        public Track(string name = "", float volume = 1, bool isMuted = false)
        {
            _Index = -1;
            _Name = name;
            _Volume = volume;
            _IsMuted = isMuted;
        }
    }
}
