using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vogen.Client.ViewModels
{
    public static class ObservableCollectionExtensions
    {
        public static void SortByInsertion<T>(this ObservableCollection<T> list, Comparison<T> compare)
        {
            for (int i = 1; i < list.Count; i++)
            {
                int j = i - 1;
                for (; j >= 0; j--)
                    if (compare(list[j], list[i]) <= 0) break;

                if (i != j + 1)
                    list.Move(i, j + 1);
            }
        }

        public static void SortByInsertion<T>(this ObservableCollection<T> list, IComparer<T>? comparer = null)
        {
            comparer ??= Comparer<T>.Default;
            SortByInsertion(list, comparer.Compare);
        }
    }
}
