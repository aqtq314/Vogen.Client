using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Vogen.Client.ViewModels.Charting;

namespace Vogen.Client.Views
{
    public partial class NoteEditPanel : UserControl
    {
        public NotePart? ActiveNotePart
        {
            get => (NotePart?)GetValue(ActiveNotePartProperty);
            set => SetValue(ActiveNotePartProperty, value);
        }

        public static DependencyProperty ActiveNotePartProperty { get; } =
            DependencyProperty.Register(nameof(ActiveNotePart), typeof(NotePart), typeof(NoteEditPanel),
                new FrameworkPropertyMetadata(null));

        public NoteEditPanel()
        {
            InitializeComponent();
        }
    }
}
