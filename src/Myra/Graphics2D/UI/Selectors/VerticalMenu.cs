using System.ComponentModel;
using Myra.Graphics2D.UI.Styles;
using System.Xml.Serialization;


#if MONOGAME || FNA
using Microsoft.Xna.Framework.Input;
#elif STRIDE
using Stride.Input;
#else
using Myra.Platform;
#endif

namespace Myra.Graphics2D.UI
{
    public class VerticalMenu : Menu
    {
        private Proportion _imageProportion = Proportion.Auto, _shortcutProportion = Proportion.Auto;

        public override Orientation Orientation
        {
            get
            {
                return Orientation.Vertical;
            }
        }

        [DefaultValue(HorizontalAlignment.Left)]
        public override HorizontalAlignment HorizontalAlignment
        {
            get
            {
                return base.HorizontalAlignment;
            }
            set
            {
                base.HorizontalAlignment = value;
            }
        }

        [DefaultValue(VerticalAlignment.Top)]
        public override VerticalAlignment VerticalAlignment
        {
            get
            {
                return base.VerticalAlignment;
            }
            set
            {
                base.VerticalAlignment = value;
            }
        }

        private bool HasImage
        {
            get
            {
                if (Orientation == Orientation.Horizontal)
                {
                    return false;
                }

                return InternalChild.ColumnsProportions[0] == _imageProportion;
            }

            set
            {
                if (Orientation == Orientation.Horizontal)
                {
                    return;
                }

                var hasImage = HasImage;
                if (hasImage == value)
                {
                    return;
                }

                if (hasImage && !value)
                {
                    InternalChild.ColumnsProportions.RemoveAt(0);
                }
                else if (!hasImage && value)
                {
                    InternalChild.ColumnsProportions.Insert(0, _imageProportion);
                }

                InvalidateMenuContent();
            }
        }

        private bool HasShortcut
        {
            get
            {
                return InternalChild.ColumnsProportions[InternalChild.ColumnsProportions.Count - 1] == _shortcutProportion;
            }

            set
            {
                var hasShortcut = HasShortcut;
                if (hasShortcut == value)
                {
                    return;
                }

                if (hasShortcut && !value)
                {
                    InternalChild.ColumnsProportions.RemoveAt(InternalChild.ColumnsProportions.Count - 1);
                }
                else if (!hasShortcut && value)
                {
                    InternalChild.ColumnsProportions.Add(_shortcutProportion);
                }

                InvalidateMenuContent();
            }
        }

        public VerticalMenu(string styleName = Stylesheet.DefaultStyleName) : base(styleName)
        {
            HorizontalAlignment = HorizontalAlignment.Left;
            VerticalAlignment = VerticalAlignment.Top;
        }

        public override void OnKeyDown(Keys k)
        {
            base.OnKeyDown(k);

            switch (k)
            {
                case Keys.Up:
                    MoveHover(-1);
                    break;
                case Keys.Down:
                    MoveHover(1);
                    break;
            }
        }

        protected override void InternalSetStyle(Stylesheet stylesheet, string name)
        {
            ApplyMenuStyle(stylesheet.VerticalMenuStyles.SafelyGetStyle(name));
        }

        protected override void UpdateWidgets()
        {
            var hasImage = false;
            var hasShortcut = false;
            foreach (var item in Items)
            {
                if (item is MenuItem menuItem)
                {
                    if (menuItem.Image != null)
                    {
                        hasImage = true;
                    }

                    if (!string.IsNullOrEmpty(menuItem.ShortcutText))
                    {
                        hasShortcut = true;
                    }
                }
            }

            HasImage = hasImage;
            HasShortcut = hasShortcut;

            base.UpdateWidgets();
        }

        protected override void PlaceMenuItemInGrid(IMenuItem item, int index)
        {
            var separatorSpan = 1;
            if (HasImage)
            {
                ++separatorSpan;
            }
            if (HasShortcut)
            {
                ++separatorSpan;
            }

            var menuItem = item as MenuItem;
            if (menuItem != null)
            {
                var colIndex = 0;
                if (this.HasImage)
                {
                    Grid.SetColumn(menuItem.ImageWidget, colIndex++);
                    Grid.SetRow(menuItem.ImageWidget, index);
                }

                Grid.SetColumn(menuItem.Label, colIndex++);
                Grid.SetRow(menuItem.Label, index);

                if (this.HasShortcut)
                {
                    Grid.SetColumn(menuItem.Shortcut, colIndex++);
                    Grid.SetRow(menuItem.Shortcut, index);
                }
            }
            else
            {
                var separator = (MenuSeparator)item;
                Grid.SetColumn(separator.Separator, 0);
                Grid.SetRow(separator.Separator, index);
                Grid.SetColumnSpan(separator.Separator, separatorSpan);
            }

            item.Index = index;

        }
    }
}