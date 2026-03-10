using Microsoft.Xna.Framework;
using Myra.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Myra.Graphics2D.UI;

public class PrimaryItemLayout<T> : ILayout where T : Widget
{
    private readonly Widget _container;

    private ObservableCollection<Widget> Children => _container.Children;

    public T Child
    {
        get;
        set
        {
            if (value is null)
            {
                if (field is not null && Children.Contains(field))
                {
                    Children.Remove(field);
                }
            }
            else
            {
                if (field is not null && Children.Contains(field))
                {
                    var childPosition = Children.IndexOf(field);
                    Children[childPosition] = value;
                }
                else
                {
                    Children.Add(value);
                }
            }

            field = value;
        }
    }

    public PrimaryItemLayout(Widget container)
    {
        _container = container ?? throw new ArgumentNullException(nameof(container));
    }

    public Point Measure(IEnumerable<Widget> widgets, Point availableSize)
    {
        var result = Mathematics.PointZero;

        if (Child != null)
        {
            result = Child.Measure(availableSize);
        }

        return result;
    }

    public void Arrange(IEnumerable<Widget> widgets, Rectangle bounds)
    {
        if (Child != null && Child.Visible)
        {
            Child.Arrange(bounds);
        }
    }
}
