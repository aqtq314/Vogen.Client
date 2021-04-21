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
        public override TextBoxPopupBase LyricPopup => lyricPopup;
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
    }
}
