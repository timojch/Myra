using Myra.Graphics2D.UI.Styles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Myra.Graphics2D.UI.Specializations;

public class Tooltip : Label
{
    public Tooltip(string styleName = Stylesheet.DefaultStyleName) 
        : base(styleName)
    {
    }
}
