﻿using System;
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

namespace Vogen.Client.Views
{
    public partial class NoteChartEditPanel : NoteChartEditPanelBase
    {
        public NoteChartEditPanel()
        {
            InitializeComponent();
            BindBehaviors(this, chartEditor, rulerGrid, sideKeyboard, hScrollZoom, vScrollZoom);

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
