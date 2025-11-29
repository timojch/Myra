using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Myra.Graphics2D.UI.Styles;

public interface IStyle
{
    void ApplyTo(Widget widget);
}

public interface IStyle<in TWidget>
    : IStyle
    where TWidget : Widget
{
    void ApplyTo(TWidget widget);

    void IStyle.ApplyTo(Widget widget) => this.ApplyTo((TWidget)widget);
}
