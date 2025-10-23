using System;
using System.ComponentModel;
using Myra.Graphics2D.UI.Styles;
using Myra.Utility;
using System.Xml.Serialization;
using Myra.Attributes;
using FontStashSharp;
using Myra.Events;

#if MONOGAME || FNA
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
#elif STRIDE
using Stride.Core.Mathematics;
using Stride.Input;
#else
using System.Drawing;
using Myra.Platform;
using Color = FontStashSharp.FSColor;
#endif

namespace Myra.Graphics2D.UI
{
    public class FloatingFrame : ContentControl
    {
        private Widget _content;
        private Widget _previousKeyboardFocus;
        private readonly StackPanelLayout _layout = new StackPanelLayout(Orientation.Vertical);
        private Desktop _subscribedDesktop;

        [Browsable(false)]
        [Content]
        public override Widget Content
        {
            get
            {
                return _content;
            }

            set
            {
                if (value == Content)
                {
                    return;
                }

                // Remove existing
                if (_content != null)
                {
                    Children.Remove(_content);
                }

                if (value != null)
                {
                    StackPanel.SetProportionType(value, ProportionType.Fill);
                    Children.Insert(Children.Count, value);
                }

                _content = value;
            }
        }

        [Browsable(false)]
        [XmlIgnore]
        public bool Result { get; set; }

        [Category("Behavior")]
        [DefaultValue(false)]
        public bool IsLightDismiss { get; set; }

        [DefaultValue(HorizontalAlignment.Left)]
        public override HorizontalAlignment HorizontalAlignment
        {
            get
            {
                return base.HorizontalAlignment;
            }
            set
            {
                base.HorizontalAlignment = value;
            }
        }

        [DefaultValue(VerticalAlignment.Top)]
        public override VerticalAlignment VerticalAlignment
        {
            get
            {
                return base.VerticalAlignment;
            }
            set
            {
                base.VerticalAlignment = value;
            }
        }

        [DefaultValue(DragDirection.Both)]
        public override DragDirection DragDirection { get => base.DragDirection; set => base.DragDirection = value; }

        [Category("Behavior")]
        [DefaultValue(Keys.Escape)]
        public Keys? CloseKey { get; set; }

        private bool IsWindowPlaced { get; set; }

        public event EventHandler<CancellableEventArgs> Closing;
        public event EventHandler Closed;

        public FloatingFrame(string styleName = Stylesheet.DefaultStyleName)
        {
            _layout.Spacing = 8;
            ChildrenLayout = _layout;

            // Set style if we are not a derived class.
            if (this.GetType() == typeof(FloatingFrame))
            {
                SetStyle(styleName);
            }
        }

        protected override void InternalArrange()
        {
            base.InternalArrange();

            if (!IsWindowPlaced)
            {
                CenterOnDesktop();
                IsWindowPlaced = true;
            }
        }

        public void CenterOnDesktop()
        {
            var size = Bounds.Size();
            Left = (ContainerBounds.Width - size.X) / 2;
            Top = (ContainerBounds.Height - size.Y) / 2;
        }

        public override void OnTouchDown()
        {
            BringToFront();
            base.OnTouchDown();
        }

        public override void OnKeyDown(Keys k)
        {
            base.OnKeyDown(k);

            if (k == CloseKey)
            {
                Close();
            }
        }

        protected override void OnPlacedChanged()
        {
            base.OnPlacedChanged();

            if (this.Desktop is not null)
            {
                this._subscribedDesktop = this.Desktop;
                this._subscribedDesktop.MouseClick += this.Desktop_MouseClick;
            }
            else if (this._subscribedDesktop is not null)
            {
                this._subscribedDesktop.MouseClick -= this.Desktop_MouseClick;
                this._subscribedDesktop = null;
            }
        }

        private void Desktop_MouseClick(object sender, PointerEventArgs e)
        {
            if (this.IsLightDismiss && this.Desktop is not null)
            {
                if (this.Bounds.Size.X > 0 && this.Bounds.Size.Y > 0 && !this.Bounds.Contains(this.ToLocal(this.Desktop.MousePosition)))
                {
                    this.Close();
                }
            }
        }

        private void InternalShow(Desktop desktop, Point? position = null)
        {
            Visible = true;
            Desktop = desktop;
            Desktop.Widgets.Add(this);

            if (position != null)
            {
                Left = position.Value.X;
                Top = position.Value.Y;
                IsWindowPlaced = true;
            }
        }

        public void Show(Desktop desktop, Point? position = null)
        {
            IsModal = false;
            InternalShow(desktop, position);
        }

        public void ShowModal(Desktop desktop, Point? position = null)
        {
            IsModal = true;
            InternalShow(desktop, position);

            _previousKeyboardFocus = desktop.FocusedKeyboardWidget;

            // Force mouse wheel focused to be set to the first appropriate widget in the next Desktop.UpdateLayout
            if (AcceptsKeyboardFocus)
            {
                Desktop.FocusedKeyboardWidget = this;
            }
        }

        public virtual void Close()
        {
            if (Desktop == null)
            {
                // Is closed already
                return;
            }

            var ev = Closing;
            if (ev != null)
            {
                var args = new CancellableEventArgs();
                ev(this, args);
                if (args.Cancel)
                {
                    return;
                }
            }

            if (IsModal)
            {
                Desktop.FocusedKeyboardWidget = _previousKeyboardFocus;
            }

            if (Desktop.Widgets.Contains(this))
            {
                RemoveFromDesktop();
            }
            else
            {
                //todo fix remove error. DONE
                RemoveFromParent();
            }

            Closed.Invoke(this);
        }

        protected override void InternalSetStyle(Stylesheet stylesheet, string name)
        {
            ApplyWidgetStyle(stylesheet.WindowStyles.SafelyGetStyle(name));
        }
    }
}