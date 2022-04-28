using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vogen.Client.ViewModels.Charting
{
    public class Track : CollectionViewModelBase<PartBase>
    {
        string _Name;
        float _Volume;
        bool _IsMuted;

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

        public Track(string? name = null, float volume = 1, bool isMuted = false,
            IEnumerable<PartBase>? parts = null)
            : base(parts)
        {
            _Name = name ?? "";
            _Volume = volume;
            _IsMuted = isMuted;
        }
    }
}
