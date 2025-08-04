using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Myra.Graphics2D.UI
{
    public class FlowLayout : ILayout
    {
        public List<FlowContainer.ControlPoint> ControlPoints { get; } = new List<FlowContainer.ControlPoint>();

        public int IndentSize = 28;

        public int LineSpacing = 4;

        public int HorizontalSpacing = 7;

        public int MinLineHeight = 0;

        public int MaxLineHeight = int.MaxValue;

        public bool Wrap = true;

        public IEnumerable<Tuple<Widget, Rectangle>> GetArrangedRectangles(IEnumerable<Widget> widgets, Point availableSize)
        {
            if (!widgets.Any())
            {
                yield break;
            }
            else if (widgets.Count() == 1)
            {
                var widget = widgets.First();
                var measure = widget.Measure(availableSize);
                yield return new Tuple<Widget, Rectangle>(widget, new Rectangle(Point.Zero, measure));
            }
            else
            {
                var cursorPos = Point.Zero;
                var lineStart = Point.Zero;
                int currentLineHeight = this.MinLineHeight;
                int currentIndent = 0;
                int itemsOnCurrentLine = 0;
                bool isHorizontalSpaceSkipped = true;

                Action funcNewLine = () =>
                {
                    isHorizontalSpaceSkipped = true;
                    lineStart = lineStart + new Point(0, currentLineHeight + this.LineSpacing);
                    cursorPos = lineStart + new Point(this.IndentSize * currentIndent, 0);
                    currentLineHeight = this.MinLineHeight;
                    itemsOnCurrentLine = 0;
                };

                Action<FlowContainer.ControlPoint> funcHandleControlPoint = (FlowContainer.ControlPoint cp) =>
                {
                    currentIndent += cp.Indent;
                    if (cp.LineBreak)
                    {
                        funcNewLine();
                    }

                    if (cp.SkipHorizontalSpacing && !isHorizontalSpaceSkipped)
                    {
                        cursorPos += new Point(-this.HorizontalSpacing, 0);
                        isHorizontalSpaceSkipped = true;
                    }
                };

                foreach (var widget in widgets)
                {
                    foreach (var cp in this.ControlPoints.Where(cp => cp.AnchorWidget == widget && cp.IsBeforeAnchor))
                    {
                        funcHandleControlPoint(cp);
                    }

                    var measure = widget.Measure(new Point(availableSize.X - cursorPos.X, MaxLineHeight));

                    if (this.Wrap && cursorPos.X + measure.X > availableSize.X)
                    {
                        // Doesn't fit. If we aren't already at the start of a new line, move to a new line.
                        if (itemsOnCurrentLine > 0)
                        {
                            funcNewLine();
                        }
                    }

                    yield return new Tuple<Widget, Rectangle>(widget, new Rectangle(cursorPos, measure));
                    cursorPos += new Point(measure.X + this.HorizontalSpacing, 0);
                    isHorizontalSpaceSkipped = false;
                    currentLineHeight = Math.Min(Math.Max(currentLineHeight, measure.Y), this.MaxLineHeight);
                    itemsOnCurrentLine++;

                    foreach (var cp in this.ControlPoints.Where(cp => cp.AnchorWidget == widget && !cp.IsBeforeAnchor))
                    {
                        funcHandleControlPoint(cp);
                    }
                }
            }
        }

        public void Arrange(IEnumerable<Widget> widgets, Rectangle bounds)
        {
            foreach (var pair in this.GetArrangedRectangles(widgets, bounds.Size))
            {
                pair.Item1.Arrange(new Rectangle(pair.Item2.Location + bounds.Location, pair.Item2.Size));
            }
        }

        public Point Measure(IEnumerable<Widget> widgets, Point availableSize)
        {
            int width = 0, height = 0;
            foreach (var pair in this.GetArrangedRectangles(widgets, availableSize))
            {
                var rect = pair.Item2;
                width = Math.Max(width, rect.Right);
                height = Math.Max(height, rect.Bottom);
            }

            return new Point(width, height);
        }
    }
}
