using System;
using System.Collections.Generic;
using System.Text;

namespace Myra.Graphics2D.UI.Misc
{
    public class TooltipRegion : Widget
    {
        protected override void OnPlacedChanged()
        {
            base.OnPlacedChanged();

            this.Width = Parent.Width;
            this.Height = Parent.Height;
        }

        public override void OnMouseEntered()
        {
            base.OnMouseEntered();
        }

        public override void OnMouseLeft()
        {
            base.OnMouseLeft();
        }
    }
}
