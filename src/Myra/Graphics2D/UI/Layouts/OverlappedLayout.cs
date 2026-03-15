using Myra.Utility;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System;

#if MONOGAME || FNA
using Microsoft.Xna.Framework;
#elif STRIDE
using Stride.Core.Mathematics;
#else
using System.Drawing;
#endif

namespace Myra.Graphics2D.UI;

public class OverlappedLayout : ILayout
{
    private readonly Widget _container;

    private ObservableCollection<Widget> Children => _container.Children;

    public OverlappedLayout(Widget container)
    {
        _container = container ?? throw new ArgumentNullException(nameof(container));
    }

    public Point Measure(IEnumerable<Widget> widgets, Point availableSize)
    {
        var result = Mathematics.PointZero;

        foreach (var child in this.Children)
        {
            if (child.Visible)
            {
                var measure = child.Measure(availableSize);
                var x = Math.Max(result.X, measure.X);
                var y = Math.Max(result.Y, measure.Y);
                result = new Point(x, y);
            }
        }

        return result;
    }

    public void Arrange(IEnumerable<Widget> widgets, Rectangle bounds)
    {
        foreach (var child in this.Children)
        {
            if (child.Visible)
            {
                child.Arrange(bounds);
            }
        }
    }
}
