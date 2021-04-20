using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Vogen.Client.Controls;
using Vogen.Client.ViewModel;

namespace Vogen.Client.Views
{
    public partial class NoteChartEditPanel : NoteChartEditPanelBase
    {
        public override ChartEditor ChartEditor => chartEditor;
        public override ChartEditorAdornerLayer ChartEditorAdornerLayer => chartEditorAdornerLayer;
        public override RulerGrid RulerGrid => rulerGrid;
        public override SideKeyboard SideKeyboard => sideKeyboard;
        public override ChartScrollZoomKitBase HScrollZoom => hScrollZoom;
        public override ChartScrollZoomKitBase VScrollZoom => vScrollZoom;
        public override Popup LyricPopup => lyricPopup;
        public override TextBox LyricTextBox => lyricTextBox;
        public override ContextMenu ChartEditorContextMenu => chartEditorContextMenu;

        public NoteChartEditPanel()
        {
            InitializeComponent();
            BindBehaviors();

            PreviewMouseDown += (sender, e) =>
            {
                Focus();
            };

            IsKeyboardFocusedChanged += (sender, e) =>
            {
                if ((bool)e.NewValue)
                    border.BorderBrush = Brushes.LightSalmon;
                else
                    border.BorderBrush = null;
            };
        }

        private void CanExecuteCmdSelectionHasLyric(object sender, CanExecuteRoutedEventArgs e)
        {
            var selection = ProgramModel.ActiveSelection.Value;
            e.CanExecute = selection.SelectedNotes.Any(note => !note.IsHyphen);
        }

        private void CanExecuteCmdHasSelection(object sender, CanExecuteRoutedEventArgs e)
        {
            var selection = ProgramModel.ActiveSelection.Value;
            e.CanExecute = selection.SelectedNotes.Count > 0;
        }

        private void OnExecuteCmdEditLyrics(object sender, ExecutedRoutedEventArgs e) =>
            EditSelectedNoteLyrics();

        private void OnExecuteCmdCut(object sender, ExecutedRoutedEventArgs e) =>
            CutSelectedNotes();

        private void OnExecuteCmdCopy(object sender, ExecutedRoutedEventArgs e) =>
            CopySelectedNotes();

        private void OnExecuteCmdPaste(object sender, ExecutedRoutedEventArgs e) =>
            Paste();

        private void OnExecuteCmdDelete(object sender, ExecutedRoutedEventArgs e) =>
            DeleteSelectedNotes(UndoNodeDescription.DeleteNote);

        private void OnExecuteCmdSelectAll(object sender, ExecutedRoutedEventArgs e) =>
            SelectAll();
    }
}
