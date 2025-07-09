using Myra.Graphics2D.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Myra.Events
{
    public class DragReceivedEventArgs : EventArgs
    {
        public Widget DroppedWidget { get; }

        public DragReceivedEventArgs(Widget droppedWidget)
        {
            this.DroppedWidget = droppedWidget;
        }
    }
}
