using System.ComponentModel;
using Myra.Graphics2D.UI.Styles;
using Myra.Graphics2D.UI.Frames;


#if MONOGAME || FNA
using Microsoft.Xna.Framework.Input;
#elif STRIDE
using Stride.Input;
#else
using Myra.Platform;
#endif

namespace Myra.Graphics2D.UI
{
    public class HorizontalMenu : Menu
    {
        public override Orientation Orientation
        {
            get { return Orientation.Horizontal; }
        }

        [DefaultValue(HorizontalAlignment.Stretch)]
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

        public HorizontalMenu(string styleName = Stylesheet.DefaultStyleName) : base(styleName)
        {
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Top;
        }

        public override void OnKeyDown(Keys k)
        {
            base.OnKeyDown(k);

            if (OpenMenuItem is null)
            {
                switch (k)
                {
                    case Keys.Left:
                        MoveHover(-1);
                        break;
                    case Keys.Right:
                        MoveHover(1);
                        break;
                }
            }
        }

        protected override void PlaceMenuItemInGrid(IMenuItem item, int index)
        {
            if (item is MenuItem menuItem)
            {
                Grid.SetColumn(menuItem.Label, index);
                Grid.SetRow(menuItem.Label, 0);
            }
            else if (item is AdvancedMenuItem advancedItem)
            {
                Grid.SetColumn(advancedItem.BodyContentPanel, index);
                Grid.SetRow(advancedItem.BodyContentPanel, 0);
            }
            else
            {
                var separator = (MenuSeparator)item;
                Grid.SetColumn(separator.Separator, index);
                Grid.SetRow(separator.Separator, 0);
            }

            item.Index = index;

            ++index;
        }
    }
}
