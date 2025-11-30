using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Myra.Graphics2D.UI.Styles;

public class AlsoKnownAsAttribute : Attribute
{
    public string Name { get; }

    public AlsoKnownAsAttribute(string name)
    {
        this.Name = name;
    }
}
