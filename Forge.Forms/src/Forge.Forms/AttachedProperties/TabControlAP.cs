using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Forge.Forms.AttachedProperties
{
    public static class TabControlAP
    {
        public static readonly DependencyProperty HeaderHeightProperty =
            DependencyProperty.RegisterAttached("HeaderHeight", typeof(GridLength), typeof(TabControlAP), new PropertyMetadata(GridLength.Auto));

        public static GridLength GetHeaderHeight(DependencyObject obj)
        {
            return (GridLength)obj.GetValue(HeaderHeightProperty);
        }

        public static void SetHeaderHeight(DependencyObject obj, GridLength value)
        {
            obj.SetValue(HeaderHeightProperty, value);
        }
    }
}