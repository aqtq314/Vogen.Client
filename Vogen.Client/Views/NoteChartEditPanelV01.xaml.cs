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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Vogen.Client.Controls;
using Vogen.Client.ViewModel;

namespace Vogen.Client.Views
{
    public partial class NoteChartEditPanelV01 : NoteChartEditPanelBase
    {
        public override ChartEditor ChartEditor => chartEditor;
        public override ChartEditorAdornerLayer ChartEditorAdornerLayer => chartEditorAdornerLayer;
        public override RulerGrid RulerGrid => rulerGrid;
        public override SideKeyboard SideKeyboard => sideKeyboard;
        public override BgAudioDisplay BgAudioDisplay => bgAudioDisplay;
        public override ChartScrollZoomKitBase HScrollZoom => hScrollZoom;
        public override ChartScrollZoomKitBase VScrollZoom => vScrollZoom;
        public override TextBoxPopupBase LyricPopup => lyricPopup;
        public override ContextMenu ChartEditorContextMenu => chartEditorContextMenu;

        public NoteChartEditPanelV01()
        {
            InitializeComponent();
            BindBehaviors();

            var focusedBrush = (Brush)Resources["focusedBrush"];
            var synthingBrush = (Brush)Resources["synthingBrush"];
            synthingBrush.Transform = new TranslateTransform();
            synthingBrush.Transform.BeginAnimation(TranslateTransform.XProperty,
                new DoubleAnimation(0, -12, new Duration(TimeSpan.FromSeconds(1))) { RepeatBehavior = RepeatBehavior.Forever });

            PreviewMouseDown += (sender, e) =>
            {
                Focus();
            };
        }
    }
}
