using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Vogen.Client.ViewModels
{
    public class NoteTrack : ViewModelBase
    {
        string _Name;
        string _SingerId;
        string _RomScheme;

        public string Name
        {
            get => _Name;
            set => SetAndNotify(ref _Name, value);
        }

        public string SingerId
        {
            get => _SingerId;
            set => SetAndNotify(ref _SingerId, value);
        }

        public string RomScheme
        {
            get => _RomScheme;
            set => SetAndNotify(ref _RomScheme, value);
        }

        public ObservableCollection<Note> Notes { get; init; }
        public ObservableCollection<NoteGroup> NoteGroups { get; init; }

        public NoteTrack(string name, string singerId, string romScheme,
            IEnumerable<Note>? notes = null, IEnumerable<NoteGroup>? noteGroups = null)
        {
            _Name = name;
            _SingerId = singerId;
            _RomScheme = romScheme;
            Notes = new ObservableCollection<Note>(notes ?? new[] { new Note(0) });
            NoteGroups = new ObservableCollection<NoteGroup>(noteGroups ?? Enumerable.Empty<NoteGroup>());
        }
    }
}
