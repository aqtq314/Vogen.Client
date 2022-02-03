using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vogen.Client.ViewModels
{
    public class Utterance : ViewModelBase
    {
        string _Name;
        string? _RomSchemeOverride;

        public string Name
        {
            get => _Name;
            set => SetAndNotify(ref _Name, value);
        }

        public string? RomSchemeOverride
        {
            get => _RomSchemeOverride;
            set => SetAndNotify(ref _RomSchemeOverride, value);
        }

        public ObservableCollection<Note> Notes { get; init; }

        public Utterance(string name, IEnumerable<Note> notes, string? romSchemeOverride = null)
        {
            _Name = name;
            _RomSchemeOverride = romSchemeOverride;
            Notes = new ObservableCollection<Note>(notes);
        }
    }
}
