using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vogen.Client.ViewModels
{
    public class Note : ViewModelBase
    {
        double _Pitch;
        string _Lyric = "";
        string _Rom = "";
        long _On;

        public double Pitch
        {
            get => _Pitch;
            set => SetAndNotify(ref _Pitch, value);
        }

        public string Lyric
        {
            get => _Lyric;
            set => SetAndNotify(ref _Lyric, value);
        }

        public string Rom
        {
            get => _Rom;
            set => SetAndNotify(ref _Rom, value);
        }

        public long On
        {
            get => _On;
            set => SetAndNotify(ref _On, value);
        }

        public bool IsHyphen => Lyric == "-";

        public static int CompareByOnset(Note n1, Note n2)
        {
            return Comparer<long>.Default.Compare(n1.On, n2.On);
        }
    }
}
