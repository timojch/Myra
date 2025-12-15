using Microsoft.Xna.Framework;
using Myra.Utility;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Myra.Graphics2D.UI.Layouts;

public class CustomLayout : ILayout
{
    public ControlPointsCollection ControlPoints { get; } = new ControlPointsCollection();

    private Dictionary<Widget, Tuple<IControlPoint, IControlPoint>> PositionConstraints = new();

    private Dictionary<Widget, Tuple<IControlPoint, IControlPoint>> SizeConstraints = new();

    public void SetPositionConstraint(Widget w, IControlPoint parentControlPoint, IControlPoint childControlPoint)
    {
        this.PositionConstraints[w] = Tuple.Create(parentControlPoint, childControlPoint);
    }

    public void ClearPositionConstraint(Widget w)
    {
        this.PositionConstraints.Remove(w);
    }

    public Tuple<IControlPoint, IControlPoint> GetPositionConstraint(Widget w)
    {
        if (this.PositionConstraints.TryGetValue(w, out var constraint))
        {
            return constraint;
        }
        else
        {
            return Tuple.Create(this.ControlPoints.Center, this.ControlPoints.Center);
        }
    }

    public void SetSizeConstraint(Widget w, IControlPoint parentControlPoint, IControlPoint childControlPoint)
    {
        this.SizeConstraints[w] = Tuple.Create(parentControlPoint, childControlPoint);
    }

    public Point Measure(IEnumerable<Widget> widgets, Point availableSize)
    {
        this.Calculate(widgets, ref availableSize, out _, false);
        return availableSize;
    }

    public void Arrange(IEnumerable<Widget> widgets, Rectangle bounds)
    {
        var availableSize = bounds.Size;
        this.Calculate(widgets, ref availableSize, out var widgetTopLefts, true);

        foreach (var pair in widgets.Index())
        {
            var widget = pair.Item;
            var widgetRect = widgetTopLefts[pair.Index];

            widget.Arrange(new Rectangle(bounds.Location + widgetRect.Location, widgetRect.Size));
        }
    }

    private void Calculate(IEnumerable<Widget> widgets, ref Point availableSize, out Rectangle[] widgetPositions, bool doSizeConstraints)
    {
        widgetPositions = new Rectangle[widgets.Count()];
        var totalSize = availableSize;
        var widgetDefaultSizes = widgets.Select(w => w.Measure(totalSize)).ToArray();
        bool hasMinimumSizeChanged = true;
        int iterations = 0;

        while (hasMinimumSizeChanged && iterations++ < 100)
        {
            Point maximumShrinkage = totalSize;
            hasMinimumSizeChanged = false;

            foreach (var pair in widgets.Index())
            {
                var size = widgetDefaultSizes[pair.Index];
                var widget = pair.Item;

                if (!widget.Visible)
                {
                    continue;
                }

                var constraint = this.GetPositionConstraint(widget);
                var topLeft = constraint.Item1.Evaluate(totalSize) - constraint.Item2.Evaluate(size);
                var bottomLeft = topLeft + size;
                if (doSizeConstraints && this.SizeConstraints.TryGetValue(widget, out var sizeConstraint))
                {
                    var controlPointCurrent = sizeConstraint.Item2.Evaluate(size);
                    var controlPointExpected = sizeConstraint.Item1.Evaluate(totalSize);
                    var necessaryDelta = controlPointExpected - controlPointCurrent;

                    // scaleImpact : If I were to double size, how much would the control point move?
                    var scaleImpact = controlPointCurrent - constraint.Item2.Evaluate(size);

                    if (scaleImpact.X == 0)
                    {
                        necessaryDelta.X = 0;
                        scaleImpact.X = 1;
                    }

                    if (scaleImpact.Y == 0)
                    {
                        necessaryDelta.Y = 0;
                        scaleImpact.Y = 1;
                    }

                    var scale = Vector2.One + new Vector2((float)necessaryDelta.X / scaleImpact.X, (float)necessaryDelta.Y / scaleImpact.Y);

                    size = new Point((int)(size.X * scale.X), (int)(size.Y * scale.Y));
                    topLeft = constraint.Item1.Evaluate(totalSize) - constraint.Item2.Evaluate(size);
                    bottomLeft = topLeft + size;
                }

                var shadowTopLeft = constraint.Item1.EvaluateShadow(totalSize) - constraint.Item2.EvaluateShadow(size);

                var minimumSize = bottomLeft - shadowTopLeft;
                var shrinkage = totalSize - minimumSize;

                widgetPositions[pair.Index] = new Rectangle(topLeft, size);

                if (shrinkage.X < 0 && shrinkage.Y < 0)
                {
                    totalSize -= shrinkage;
                    hasMinimumSizeChanged = true;
                }
                else if (shrinkage.X < 0)
                {
                    totalSize -= new Point(shrinkage.X, 0);
                    hasMinimumSizeChanged = true;
                }
                else if (shrinkage.Y < 0)
                {
                    totalSize -= new Point(0, shrinkage.Y);
                    hasMinimumSizeChanged = true;
                }
                else
                {
                    maximumShrinkage = new Point(Math.Min(shrinkage.X, maximumShrinkage.X), Math.Min(shrinkage.Y, maximumShrinkage.Y));
                }
            }

            if (maximumShrinkage != Point.Zero)
            {
                totalSize -= maximumShrinkage;
                hasMinimumSizeChanged = true;
            }
        }

        availableSize = totalSize;
    }

    public class ControlPointsCollection
    {
        public IControlPoint TopLeft { get; }
        public IControlPoint TopCenter { get; }
        public IControlPoint TopRight { get; }
        public IControlPoint CenterLeft { get; }
        public IControlPoint Center { get; }
        public IControlPoint CenterRight { get; }
        public IControlPoint BottomLeft { get; }
        public IControlPoint BottomCenter { get; }
        public IControlPoint BottomRight { get; }

        public ControlPointsCollection()
        {
            this.TopLeft = new FixedControlPoint(Point.Zero);
            this.TopCenter = this.TopLeft.OffsetByProportion(0.5f, 0);
            this.TopRight = this.TopLeft.OffsetByProportion(1, 0);
            this.CenterLeft = this.TopLeft.OffsetByProportion(0, 0.5f);
            this.Center = this.TopLeft.OffsetByProportion(0.5f, 0.5f);
            this.CenterRight = this.TopLeft.OffsetByProportion(1, 0.5f);
            this.BottomLeft = this.TopLeft.OffsetByProportion(0, 1);
            this.BottomCenter = this.TopLeft.OffsetByProportion(0.5f, 1);
            this.BottomRight = this.TopLeft.OffsetByProportion(1, 1);
        }
    }

    public interface IControlPoint
    {
        /// <summary>
        /// Calculates where this control point would be positioned within a rectangle of the given size.
        /// </summary>
        /// <param name="availableSize">The size</param>
        /// <returns>The position of the control point within that size</returns>
        Point Evaluate(Point availableSize);

        /// <summary>
        /// Gets a control point that is exactly opposite this one.
        /// </summary>
        IControlPoint Opposite { get; }
    }

    internal class FixedControlPoint : IControlPoint
    {
        public Point Location { get; }

        public FixedControlPoint(Point location)
        {
            Location = location;
        }

        public Point Evaluate(Point availableSize)
        {
            return this.Location;
        }

        public IControlPoint Opposite
        {
            get
            {
                return new FixedControlPoint(Point.Zero - Location).OffsetByProportion(1, 1);
            }
        }

        public override string ToString()
        {
            var x = (int)(Location.X * 2);
            var y = (int)(Location.Y * 2);
            return (x, y) switch
            {
                (0, 0) => "TopLeft",
                (0, 1) => "CenterLeft",
                (0, 2) => "BottomLeft",
                (1, 0) => "TopCenter",
                (1, 1) => "Center",
                (1, 2) => "BottomCenter",
                (2, 0) => "TopRight",
                (2, 1) => "CenterRight",
                (2, 2) => "BottomRight",
                _ => "FixedPoint"
            };
        }
    }

    internal class ProportionalOffsetControlPoint : IControlPoint
    {
        public IControlPoint Parent { get; }

        public Vector2 Offset { get; }

        public ProportionalOffsetControlPoint(IControlPoint parent, Vector2 offset)
        {
            this.Parent = parent;
            this.Offset = offset;
        }

        public Point Evaluate(Point availableSize)
        {
            var offsetPoint = new Vector2(this.Offset.X * availableSize.X, this.Offset.Y * availableSize.Y).ToPoint();
            var ret = this.Parent.Evaluate(availableSize) + offsetPoint;

            return ret;
        }

        public IControlPoint Opposite
        {
            get
            {
                return this.Parent.Opposite.OffsetByProportion(Vector2.Zero - this.Offset);
            }
        }

        public override string ToString() => $"{this.Parent} + Proportionally {this.Offset}";
    }

    internal class FixedValueOffsetControlPoint : IControlPoint
    {
        public IControlPoint Parent { get; }

        public Point Offset { get; }

        public FixedValueOffsetControlPoint(IControlPoint parent, Point offset)
        {
            this.Parent = parent;
            this.Offset = offset;
        }

        public Point Evaluate(Point availableSize)
        {
            var ret = this.Parent.Evaluate(availableSize) + this.Offset;

            return ret;
        }

        public IControlPoint Opposite
        {
            get
            {
                return this.Parent.Opposite.OffsetByFixedValue(Point.Zero - this.Offset);
            }
        }

        public override string ToString() => $"{this.Parent} + Exactly {this.Offset}";
    }
}

public static class ControlPointExtensions
{
    public static CustomLayout.IControlPoint OffsetByProportion(this CustomLayout.IControlPoint start, Vector2 offset)
    {
        return new CustomLayout.ProportionalOffsetControlPoint(start, offset);
    }

    public static CustomLayout.IControlPoint OffsetByProportion(this CustomLayout.IControlPoint start, float xOffset, float yOffset)
    {
        return start.OffsetByProportion(new Vector2(xOffset, yOffset));
    }

    public static CustomLayout.IControlPoint OffsetByFixedValue(this CustomLayout.IControlPoint start, Point offset)
    {
        return new CustomLayout.FixedValueOffsetControlPoint(start, offset);
    }

    public static CustomLayout.IControlPoint OffsetByFixedValue(this CustomLayout.IControlPoint start, int xOffset, int yOffset)
    {
        return start.OffsetByFixedValue(new Point(xOffset, yOffset));
    }

    /// <summary>
    /// Evaluates where a control point exactly opposite this control point would be placed.
    /// </summary>
    /// <param name="availableSize">The availabel size</param>
    /// <returns>The poisition of an opposite control point</returns>
    public static Point EvaluateShadow(this CustomLayout.IControlPoint start, Point availableSize)
    {
        return start.Opposite.Evaluate(availableSize);
    }
}
