using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Myra.Graphics2D.UI
{
    public class ContentPanel : ContentControl
    {
        private SingleItemLayout<Widget> _layout;

        public override Widget Content
        {
            get => this._layout.Child;
            set => this._layout.Child = value;
        }

        public ContentPanel()
        {
            this._layout = new SingleItemLayout<Widget>(this);
            this.ChildrenLayout = this._layout;
        }
    }
}
