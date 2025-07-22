using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Myra.Events;
public class PointerEventArgs
{
    public int ButtonIndex { get; }

    public PointerEventArgs(int buttonIndex)
    {
        this.ButtonIndex = buttonIndex;
    }
}
