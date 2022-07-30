using Doaz.Reactive;
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
using Vogen.Client.Model;
using Vogen.Client.ViewModels.Charting;
using Vogen.Client.ViewModels.Utils;
using Vogen.Client.Views;

namespace Vogen.Client.Controls
{
    /// <summary>
    /// 按照步骤 1a 或 1b 操作，然后执行步骤 2 以在 XAML 文件中使用此自定义控件。
    ///
    /// 步骤 1a) 在当前项目中存在的 XAML 文件中使用该自定义控件。
    /// 将此 XmlNamespace 特性添加到要使用该特性的标记文件的根
    /// 元素中:
    ///
    ///     xmlns:MyNamespace="clr-namespace:Vogen.Client.Controls"
    ///
    ///
    /// 步骤 1b) 在其他项目中存在的 XAML 文件中使用该自定义控件。
    /// 将此 XmlNamespace 特性添加到要使用该特性的标记文件的根
    /// 元素中:
    ///
    ///     xmlns:MyNamespace="clr-namespace:Vogen.Client.Controls;assembly=Vogen.Client.Controls"
    ///
    /// 您还需要添加一个从 XAML 文件所在的项目到此项目的项目引用，
    /// 并重新生成以避免编译错误:
    ///
    ///     在解决方案资源管理器中右击目标项目，然后依次单击
    ///     “添加引用”->“项目”->[浏览查找并选择此项目]
    ///
    ///
    /// 步骤 2)
    /// 继续操作并在 XAML 文件中使用控件。
    ///
    ///     <MyNamespace:ValueBox/>
    ///
    /// </summary>
    [TemplatePart(Name = "PART_TextBox", Type = typeof(TextBox))]
    public class ValueBox : Thumb
    {
        static ValueBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ValueBox), new FrameworkPropertyMetadata(typeof(ValueBox)));
        }

        TextBox? PART_TextBox = null;


        public TimeSignature Value
        {
            get { return (TimeSignature)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(TimeSignature), typeof(ValueBox),
                new FrameworkPropertyMetadata(new TimeSignature(4, 4)));

        public override void OnApplyTemplate()
        {
            if (PART_TextBox != null)
            {
                PART_TextBox.KeyDown -= OnPARTTextBoxKeyDown;
            }

            base.OnApplyTemplate();
            PART_TextBox = GetTemplateChild("PART_TextBox") as TextBox;

            if (PART_TextBox != null)
            {
                PART_TextBox.KeyDown += OnPARTTextBoxKeyDown;
            }
        }

        private void OnPARTTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (PART_TextBox != null)
            {
                if (e.Key == Key.Enter)
                {
                    this.Value = TimeSignature.TryParse(PART_TextBox.Text).GetOrDefault(this.Value);
                    PART_TextBox.Visibility = Visibility.Collapsed;
                }
                if (e.Key == Key.Escape)
                {
                    PART_TextBox.Visibility = Visibility.Collapsed;
                }
            }
        }

        public ValueBox()
        {
            this.DragStarted += OnValueBoxDragStarted;
            this.DragDelta += OnValueBoxDragDelta;
            this.DragCompleted += OnValueBoxDragCompleted;
        }

        private int NumeratorOrigin;
        private int DenominatorOrigin;
        private void OnValueBoxDragStarted(object sender, DragStartedEventArgs e)
        {
            NumeratorOrigin = this.Value.Numerator;
            DenominatorOrigin = this.Value.Denominator;
        }

        private void OnValueBoxDragDelta(object sender, DragDeltaEventArgs e)
        {
            int NumeratorTmp = NumeratorOrigin + (int)e.HorizontalChange / 50;
            int x = (int)e.VerticalChange / 50;
            int DenominatorTmp = DenominatorOrigin;
            while (x != 0)
            {
                DenominatorTmp = x > 0 ? DenominatorTmp * 2 : DenominatorTmp / 2;
                if (DenominatorTmp >= 128) break;
                x = x > 0 ? x - 1 : x + 1;
            }
            var NumeratorNew = NumeratorTmp < 1 ? 1 : NumeratorTmp > 256 ? 256 : NumeratorTmp;
            var DenominatorNew = DenominatorTmp < 1 ? 1 : DenominatorTmp > 128 ? 128 : DenominatorTmp;
            this.Value = new TimeSignature(NumeratorNew, DenominatorNew);
        }

        private void OnValueBoxDragCompleted(object sender, DragCompletedEventArgs e)
        {
            if (e.HorizontalChange < 10 && e.HorizontalChange > -10 && e.VerticalChange < 10 && e.VerticalChange > -10)
                if (PART_TextBox != null)
                {
                    PART_TextBox.Text = Value.ToString();
                    PART_TextBox.Visibility = Visibility.Visible;
                    PART_TextBox.Focus();
                }
        }
    }
}
