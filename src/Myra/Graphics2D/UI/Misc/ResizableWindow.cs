using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI.Styles;
using Myra.Utility;
using System;
using System.Collections.Generic;
using System.Text;

namespace Myra.Graphics2D.UI.Misc
{
    public class ResizableWindow : Window
    {
        public override Desktop Desktop
        {
            get => base.Desktop;
            internal set
            {
                if (base.Desktop != value)
                {
                    this.UnsubscribeDesktop();
                    base.Desktop = value;
                    this.SubscribeDesktop();
                }
            }
        }

        private bool IsSubscribed = false;

        private bool IsResizing
        {
            get => this.ResizeStartPosition.HasValue;
            set
            {
                if (value)
                {
                    this.ResizeStartPosition = this.Desktop.ToLocal(new Vector2(Desktop.TouchPosition.Value.X, Desktop.TouchPosition.Value.Y));
                    this.ResizeStartSize = this.Bounds.Size;
                }
                else
                {
                    this.ResizeStartPosition = null;
                    this.ResizeStartSize = null;
                }
            }
        }

        private Vector2? ResizeStartPosition;

        private Point? ResizeStartSize;

        public virtual Rectangle ResizeRegion
        {
            get
            {
                var bottomRight = this.Bounds.Location + this.Bounds.Size;
                var size = new Point(24, 24);
                var topLeft = bottomRight - size;

                return new Rectangle(topLeft, size);
            }
        }

        public override void OnTouchDown()
        {
            base.OnTouchDown();

            var touchPosition = this.Desktop.TouchPosition ?? throw new InvalidOperationException("A ResizeableWindow received an OnTouchDown event without a touch position");
            if (this.ResizeRegion.Contains(this.ToLocal(touchPosition)))
            {

            }
        }

        protected override void InternalSetStyle(Stylesheet stylesheet, string name)
        {
            base.InternalSetStyle(stylesheet, name);
            var style = stylesheet.WindowStyles.SafelyGetStyle(name);
            this.Background = style.ResizableBackground;
        }

        private void UnsubscribeDesktop()
        {
            if (this.IsSubscribed)
            {
                this.Desktop.TouchMoved -= this.OnDesktopTouchMoved;
                this.Desktop.TouchUp -= this.OnDesktopTouchUp;

                this.IsSubscribed = false;
            }
        }

        private void SubscribeDesktop()
        {
            if (this.IsSubscribed)
            {
                throw new InvalidOperationException("A ResizableWindow can only subscribe to one desktop at a time.");
            }

            if (this.Desktop != null)
            {
                this.Desktop.TouchMoved += this.OnDesktopTouchMoved;
                this.Desktop.TouchUp += this.OnDesktopTouchUp;

                this.IsSubscribed = true;
            }
        }

        private void OnDesktopTouchMoved(object sender, EventArgs args)
        {
            if (this.IsResizing)
            {
                var newPos = this.Desktop.ToLocal(new Vector2(Desktop.TouchPosition.Value.X, Desktop.TouchPosition.Value.Y));
                var delta = newPos - this.ResizeStartPosition.Value;

                this.Width = this.ResizeStartSize.Value.X + (int)delta.X;
                this.Height = this.ResizeStartSize.Value.Y + (int)delta.Y;
            }
        }

        private void OnDesktopTouchUp(object sender, EventArgs args)
        {
            if (this.IsResizing)
            {
                this.IsResizing = false;
            }
        }
    }
}
