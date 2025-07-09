using Myra.Graphics2D.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Myra.Events
{
    public class DragDroppedEventArgs : EventArgs
    {
        public bool WasCancelled { get => this.Target == null; }

        public Widget Target { get; }

        public DragDroppedEventArgs(Widget target = null)
        {
            this.Target = target;
        }
    }
}
