using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vogen.Client.ViewModels
{
    public class NoteTrack : ViewModelBase
    {
        string _SingerId;
        string _RomScheme;
        long _Off;

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

        public long Off
        {
            get => _Off;
            set => SetAndNotify(ref _Off, value);
        }

        public ObservableCollection<Note> Notes { get; init; }

        public NoteTrack(string singerId, string romScheme, long off, IEnumerable<Note>? notes = null)
        {
            _SingerId = singerId;
            _RomScheme = romScheme;
            _Off = off;
            Notes = new ObservableCollection<Note>(notes ?? Enumerable.Empty<Note>());
        }
    }
}
