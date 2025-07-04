using Myra.Attributes;
using Myra.Graphics2D.UI.Layouts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Myra.Graphics2D.UI.Containers
{
    public class FlowContainer : Container
    {
        private readonly ObservableCollection<Widget> _widgets = new ObservableCollection<Widget>();
        private readonly FlowLayout _layout = new FlowLayout();
        private bool _childrenDirty = false;

        [Content]
        [Browsable(false)]
        public override ObservableCollection<Widget> Widgets => _widgets;

        public List<ControlPoint> ControlPoints { get => this._layout.ControlPoints; }

        public FlowContainer()
        {
            this._widgets.CollectionChanged += OnWidgetsChanged;
            this.ChildrenLayout = _layout;
        }

        public ControlPoint AddLineBreak(int indent = 0)
        {
            if (this.Widgets.Count > 0)
            {
                return this.AddLineBreakAfter(this.Widgets.Last(), indent);
            }
            else
            {
                return null;
            }
        }

        public ControlPoint AddLineBreakAfter(Widget widget, int indent = 0)
        {
            var cp = new ControlPoint
            {
                AnchorWidget = widget,
                Indent = indent,
                LineBreak = true
            };
            this.ControlPoints.Add(cp);
            return cp;
        }

        private void OnWidgetsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                this.ControlPoints.Clear();
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                this.OnWidgetsRemoved(e.OldStartingIndex, e.OldItems);
            }
            else if (e.Action == NotifyCollectionChangedAction.Replace)
            {
                if (e.OldItems.Count == e.NewItems.Count)
                {
                    for (int i = 0; i < e.OldItems.Count; ++i)
                    {
                        var oldAnchor = e.OldItems[i];
                        var newAnchor = e.NewItems[i];

                        foreach (var cp in this.ControlPoints.Where(cp => cp.AnchorWidget == oldAnchor))
                        {
                            cp.AnchorWidget = (Widget)newAnchor;
                        }
                    }
                }
                else
                {
                    this.OnWidgetsRemoved(e.OldStartingIndex, e.OldItems);
                }
            }

            _childrenDirty = true;
            this.InvalidateMeasure();
        }

        protected override void InternalArrange()
        {
            if (this._childrenDirty)
            {
                this.Children.Clear();
                foreach (var widget in this.Widgets)
                {
                    this.Children.Add(widget);
                }

                this._childrenDirty = false;
            }

            base.InternalArrange();
        }

        private void OnWidgetsRemoved(int startIndex, IList removedItems)
        {
            var affectedPoints = this.ControlPoints.Where(cp => removedItems.Contains(cp.AnchorWidget)).ToArray();
            var pointsToRemove = affectedPoints.Where(cp => !cp.Persist);
            var pointsToMoveForward = affectedPoints.Where(cp => cp.Persist && cp.IsBeforeAnchor);
            var pointsToMoveBackward = affectedPoints.Where(cp => cp.Persist && !cp.IsBeforeAnchor);

            int backwardPoint = startIndex - 1;
            int forwardPoint = startIndex;

            foreach (var cp in pointsToRemove)
            {
                this.ControlPoints.Remove(cp);
            }

            if (backwardPoint >= 0)
            {
                foreach (var cp in pointsToMoveBackward)
                {
                    cp.AnchorWidget = this.Widgets[backwardPoint];
                }
            }

            if (forwardPoint < this.Widgets.Count)
            {
                foreach (var cp in pointsToMoveForward)
                {
                    cp.AnchorWidget = this.Widgets[forwardPoint];
                }
            }

        }

        public class ControlPoint
        {
            public Widget AnchorWidget;

            public bool IsBeforeAnchor;

            public bool LineBreak;

            public bool Persist;

            public int Indent;

            public bool SkipHorizontalSpacing;
        }
    }
}
