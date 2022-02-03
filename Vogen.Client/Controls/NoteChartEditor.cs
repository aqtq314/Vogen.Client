using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Vogen.Client.Controls
{
    [StyleTypedProperty(Property = "ItemContainerStyle", StyleTargetType = typeof(NoteItem))]
    public class NoteChartEditor : ItemsControl
    {
        public const double DefaultQuarterWidth = 128;
        public const double DefaultKeyHeight = 16;
        public const double DefaultMinKey = 33;
        public const double DefaultMaxKey = 93;
        public const double DefaultVOffset = 60;

        protected override bool IsItemItsOwnContainerOverride(object item) => item is NoteItem;
        protected override DependencyObject GetContainerForItemOverride() => new NoteItem();

        public static double GetQuarterWidth(DependencyObject obj) => (double)obj.GetValue(QuarterWidthProperty);
        public static void SetQuarterWidth(DependencyObject obj, double value) => obj.SetValue(QuarterWidthProperty, value);
        public static DependencyProperty QuarterWidthProperty { get; } =
            DependencyProperty.RegisterAttached("QuarterWidth", typeof(double), typeof(NoteChartEditor),
                new FrameworkPropertyMetadata(DefaultQuarterWidth,
                    FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public static double GetKeyHeight(DependencyObject obj) => (double)obj.GetValue(KeyHeightProperty);
        public static void SetKeyHeight(DependencyObject obj, double value) => obj.SetValue(KeyHeightProperty, value);
        public static DependencyProperty KeyHeightProperty { get; } =
            DependencyProperty.RegisterAttached("KeyHeight", typeof(double), typeof(NoteChartEditor),
                new FrameworkPropertyMetadata(DefaultKeyHeight,
                    FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public static double GetMinKey(DependencyObject obj) => (double)obj.GetValue(MinKeyProperty);
        public static void SetMinKey(DependencyObject obj, double value) => obj.SetValue(MinKeyProperty, value);
        private static void OnMinKeyChanged(DependencyObject d, double oldValue, double newValue) { d.CoerceValue(VOffsetProperty); }
        public static DependencyProperty MinKeyProperty { get; } =
            DependencyProperty.RegisterAttached("MinKey", typeof(double), typeof(NoteChartEditor),
                new FrameworkPropertyMetadata(DefaultMinKey,
                    FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender,
                    (d, e) => OnMinKeyChanged(d, (double)e.OldValue, (double)e.NewValue)));

        public static double GetMaxKey(DependencyObject obj) => (double)obj.GetValue(MaxKeyProperty);
        public static void SetMaxKey(DependencyObject obj, double value) => obj.SetValue(MaxKeyProperty, value);
        private static void OnMaxKeyChanged(DependencyObject d, double oldValue, double newValue) { d.CoerceValue(VOffsetProperty); }
        public static DependencyProperty MaxKeyProperty { get; } =
            DependencyProperty.RegisterAttached("MaxKey", typeof(double), typeof(NoteChartEditor),
                new FrameworkPropertyMetadata(DefaultMaxKey,
                    FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender,
                    (d, e) => OnMaxKeyChanged(d, (double)e.OldValue, (double)e.NewValue)));

        public static double GetHOffset(DependencyObject obj) => (double)obj.GetValue(HOffsetProperty);
        public static void SetHOffset(DependencyObject obj, double value) => obj.SetValue(HOffsetProperty, value);
        public static double CoerceHOffset(DependencyObject d, double _HOffset) => Math.Max(0, _HOffset);
        public static DependencyProperty HOffsetProperty { get; } =
            DependencyProperty.RegisterAttached("HOffset", typeof(double), typeof(NoteChartEditor),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsMeasure, (d, e) => { },
                    (d, baseValue) => CoerceHOffset(d, (double)baseValue)));

        public static double GetVOffset(DependencyObject obj) => (double)obj.GetValue(VOffsetProperty);
        public static void SetVOffset(DependencyObject obj, double value) => obj.SetValue(VOffsetProperty, value);
        public static double CoerceVOffset(DependencyObject d, double _VOffset) => Math.Max(GetMinKey(d), Math.Min(GetMaxKey(d), _VOffset));
        public static DependencyProperty VOffsetProperty { get; } =
            DependencyProperty.RegisterAttached("VOffset", typeof(double), typeof(NoteChartEditor),
                new FrameworkPropertyMetadata(DefaultVOffset, FrameworkPropertyMetadataOptions.AffectsMeasure, (d, e) => { },
                    (d, baseValue) => CoerceVOffset(d, (double)baseValue)));
    }
}
