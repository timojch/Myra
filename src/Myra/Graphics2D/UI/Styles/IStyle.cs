using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Myra.Graphics2D.UI.Styles;

public interface IStyle
{
    string Name { get; }

    void ApplyTo(Widget widget);

    bool CanApplyTo(Widget widget);

    IStyle Clone();
}

public interface IStyle<in TWidget>
    : IStyle
    where TWidget : Widget
{
    void ApplyTo(TWidget widget);

    void IStyle.ApplyTo(Widget widget) => this.ApplyTo((TWidget)widget);

    bool IStyle.CanApplyTo(Widget widget) => widget is TWidget;
}
