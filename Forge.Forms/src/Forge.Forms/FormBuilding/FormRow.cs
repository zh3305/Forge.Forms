using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Xml.Linq;
using Forge.Forms.Controls.Internal;
using Forge.Forms.DynamicExpressions;
using Forge.Forms.DynamicExpressions.ValueConverters;

namespace Forge.Forms.FormBuilding
{
    public class FormRow
    {
        public FormRow()
            : this(true, 1)
        {
        }

        public FormRow(bool startsNewRow, int rowSpan)
        {
            StartsNewRow = startsNewRow;
            RowSpan = rowSpan;
            Elements = new List<FormElementContainer>();
        }

        public bool StartsNewRow { get; }

        public int RowSpan { get; }

        public List<FormElementContainer> Elements { get; }
    }

    public class FormElementContainer
    {
        public FormElementContainer(int column, int columnSpan, FormElement element)
            : this(column, columnSpan, new List<FormElement> { element })
        {
        }

        public FormElementContainer(int column, int columnSpan, List<FormElement> elements)
        {
            Column = column;
            ColumnSpan = columnSpan;
            Elements = elements;
        }

        internal FormElementContainer(int column, int columnSpan, ILayout layout)
        {
            Column = column;
            ColumnSpan = columnSpan;
            Elements = layout.GetElements().ToList();
            Layout = layout;
        }

        public int Column { get; }

        public int ColumnSpan { get; }

        public List<FormElement> Elements { get; }

        // This is not ready for public API
        public ILayout Layout { get; }
    }

    /// <summary>
    /// Supports custom layout of form elements.
    /// </summary>
    public interface ILayout
    {
        IEnumerable<FormElement> GetElements();
        

        FrameworkElement Build(Func<FormElement, FrameworkElement> elementBuilder,
            IResourceContext context,
            IDictionary<string, IValueProvider> formResources);
    }

    public abstract class BaseLayout : ILayout
    {
        protected BaseLayout()
        {
            IsVisible = LiteralValue.True;
        }
        /// <summary>
        /// Gets or sets the bool resource that determines whether this element will be visible.
        /// </summary>
        public IValueProvider IsVisible { get; set; }

        public abstract FrameworkElement Build(Func<FormElement, FrameworkElement> elementBuilder,
            IResourceContext context,
            IDictionary<string, IValueProvider> formResources);
        public abstract IEnumerable<FormElement> GetElements();
        protected virtual FrameworkElement UpdateBindings(FrameworkElement element,
            IResourceContext context,
            IDictionary<string, IValueProvider> formResources)
        {
            if (IsVisible != null)
            {
                var visibility = IsVisible.ProvideValue(context);
                switch (visibility)
                {
                    case bool b:
                        element.Visibility = b ? Visibility.Visible : Visibility.Collapsed;
                        break;
                    case Visibility v:
                        element.Visibility = v;
                        break;
                    case BindingBase bindingBase:
                        if (bindingBase is Binding binding)
                        {
                            binding.Converter = new BoolOrVisibilityConverter(binding.Converter);
                        }

                        BindingOperations.SetBinding(element, UIElement.VisibilityProperty, bindingBase);
                        break;
                }
            }
            return element;
        }
    }

    public class GridLayout : BaseLayout
    {
        public GridLayout(IEnumerable<GridColumnLayout> columns, double top, double bottom) : base()
        {
            Columns = columns?.ToList() ?? new List<GridColumnLayout>(0);
            Top = top;
            Bottom = bottom;
        }

        public List<GridColumnLayout> Columns { get; }

        public double Top { get; }

        public double Bottom { get; }

        public override IEnumerable<FormElement> GetElements() => Columns.SelectMany(c => c.GetElements());

        public override FrameworkElement Build(Func<FormElement, FrameworkElement> elementBuilder,
            IResourceContext context,
            IDictionary<string, IValueProvider> formResources)
        {
            ColumnDefinition GetDefinition(double size)
            {
                if (size > 0d)
                {
                    return new ColumnDefinition
                    {
                        Width = new GridLength(size, GridUnitType.Star)
                    };
                }

                if (size < 0d)
                {
                    return new ColumnDefinition
                    {
                        Width = new GridLength(-size, GridUnitType.Pixel)
                    };
                }

                return null;
            }

            var grid = new Grid
            {
                Margin = new Thickness(0d, Top, 0d, Bottom)
            };

            var colnum = 0;
            foreach (var column in Columns)
            {
                if (column.Width == 0d)
                {
                    continue;
                }

                var gridColumn = GetDefinition(column.Left);
                if (gridColumn != null)
                {
                    grid.ColumnDefinitions.Add(gridColumn);
                    colnum++;
                }

                grid.ColumnDefinitions.Add(GetDefinition(column.Width));
                var child = column.Build(elementBuilder,context,formResources);
                Grid.SetColumn(child, colnum);
                grid.Children.Add(child);
                colnum++;
                gridColumn = GetDefinition(column.Right);
                if (gridColumn != null)
                {
                    grid.ColumnDefinitions.Add(gridColumn);
                    colnum++;
                }
            }

            return UpdateBindings(grid,context,formResources);
        }
    }

    public class Layout : BaseLayout
    {
        public Layout(IEnumerable<ILayout> children)
            : this(children, new Thickness(), VerticalAlignment.Stretch, HorizontalAlignment.Stretch)
        {
        }

        public Layout(IEnumerable<ILayout> children, Thickness margin, VerticalAlignment verticalAlignment, HorizontalAlignment horizontalAlignment) : base()
        {
            Children = children?.ToList() ?? new List<ILayout>(0);
            Margin = margin;
            VerticalAlignment = verticalAlignment;
            HorizontalAlignment = horizontalAlignment;
        }

        public List<ILayout> Children { get; }

        public Thickness Margin { get; set; }

        public VerticalAlignment VerticalAlignment { get; }

        public HorizontalAlignment HorizontalAlignment { get; }

        public override IEnumerable<FormElement> GetElements() => Children.SelectMany(c => c.GetElements());

        public override FrameworkElement Build(Func<FormElement, FrameworkElement> elementBuilder,
            IResourceContext context,
            IDictionary<string, IValueProvider> formResources)
        {
            var panel = new StackPanel
            {
                Margin = Margin,
                VerticalAlignment = VerticalAlignment,
                HorizontalAlignment = HorizontalAlignment
            };

            foreach (var child in Children)
            {
                panel.Children.Add(child.Build(elementBuilder,context,formResources));
            }

            return UpdateBindings(panel,context,formResources);
        }
    }

    public class GridColumnLayout : BaseLayout
    {
        public GridColumnLayout(ILayout child, double width, double left, double right) : base()
        {
            Child = child;
            Width = width;
            Left = left;
            Right = right;
        }

        public ILayout Child { get; }

        public double Width { get; }

        public double Left { get; }

        public double Right { get; }

        public override IEnumerable<FormElement> GetElements() => Child.GetElements();

        public override FrameworkElement Build(Func<FormElement, FrameworkElement> elementBuilder,
            IResourceContext context,
            IDictionary<string, IValueProvider> formResources)
        {
            return Child.Build(elementBuilder,context,formResources);
        }
    }

    public class FormElementLayout : BaseLayout
    {
        public FormElementLayout(FormElement element) : base()
        {
            Element = element;
        }

        public FormElement Element { get; }

        public override IEnumerable<FormElement> GetElements() => new[] { Element };

        public override FrameworkElement Build(Func<FormElement, FrameworkElement> elementBuilder,
            IResourceContext context,
            IDictionary<string, IValueProvider> formResources)
        {
            return elementBuilder(Element);
        }
    }

    public class InlineLayout : BaseLayout
    {
        public InlineLayout(IEnumerable<ILayout> elements, double top, double bottom):base()
        {
            Elements = elements?.ToList() ?? new List<ILayout>(0);
            Top = top;
            Bottom = bottom;
        }

        public List<ILayout> Elements { get; }

        public double Top { get; }

        public double Bottom { get; }

        public override IEnumerable<FormElement> GetElements() => Elements.SelectMany(e => e.GetElements());

        public override FrameworkElement Build(Func<FormElement, FrameworkElement> elementBuilder,
            IResourceContext context,
            IDictionary<string, IValueProvider> formResources)
        {
            var panel = new ActionPanel
            {
                Margin = new Thickness(0d, Top, 0d, Bottom)
            };

            foreach (var element in Elements)
            {
                if (element is FormElementLayout formElementLayout)
                {
                    var contentPresenter = elementBuilder(formElementLayout.Element);
                    ActionPanel.SetPosition(contentPresenter, formElementLayout.Element.LinePosition);
                    panel.Children.Add(contentPresenter);
                }
                else
                {
                    panel.Children.Add(element.Build(elementBuilder,context,formResources));
                }
            }

            return UpdateBindings(panel,context,formResources);
        }
    }

    public static class LayoutExtensions
    {
        public static TLayout WithBaseValueProvider<TLayout>(this TLayout layout,XElement xElement)
            where TLayout : BaseLayout
        {
            var definition = xElement.TryGetAttribute("visible");
            if (definition != null && layout != null)
            {
                layout.IsVisible = Utilities.GetResource<bool>(definition, true, Deserializers.Boolean);
            }
            return layout;
        }
    }
}
