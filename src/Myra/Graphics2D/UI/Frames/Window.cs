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
    public class Window : FloatingFrame
    {
        private readonly Label _titleLabel;

        [Category("Appearance")]
        public string Title
        {
            get
            {
                return _titleLabel.Text;
            }

            set
            {
                _titleLabel.Text = value;
            }
        }

        [Category("Appearance")]
        [StylePropertyPath("TitleStyle/TextColor")]
        public Color TitleTextColor
        {
            get
            {
                return _titleLabel.TextColor;
            }
            set
            {
                _titleLabel.TextColor = value;
            }
        }

        [Category("Appearance")]
        public SpriteFontBase TitleFont
        {
            get
            {
                return _titleLabel.Font;
            }
            set
            {
                _titleLabel.Font = value;
            }
        }

        [Category("Behavior")]
        public bool CanBeClosed
        {
            get => this.CloseButton.Visible;
            set => this.CloseButton.Visible = value;
        }

        [Browsable(false)]
        [XmlIgnore]
        public HorizontalStackPanel TitlePanel { get; private set; }

        [Browsable(false)]
        [XmlIgnore]
        public Button CloseButton { get; private set; }

        public Window(string styleName = Stylesheet.DefaultStyleName)
            : base(styleName)
        {
            AcceptsKeyboardFocus = true;
            CloseKey = Keys.Escape;

            DragDirection = DragDirection.Both;

            Result = false;
            HorizontalAlignment = HorizontalAlignment.Left;
            VerticalAlignment = VerticalAlignment.Top;

            TitlePanel = new HorizontalStackPanel
            {
                Spacing = 8
            };
            DragHandle = TitlePanel;

            _titleLabel = new Label();
            _titleLabel.AutoEllipsisMethod = FontStashSharp.RichText.AutoEllipsisMethod.Character;
            _titleLabel.AutoEllipsisString = "…";
            StackPanel.SetProportionType(_titleLabel, ProportionType.Fill);
            TitlePanel.Widgets.Add(_titleLabel);

            CloseButton = new Button
            {
                Content = new Image()
            };

            CloseButton.Click += (sender, args) =>
            {
                Close();
            };

            TitlePanel.Widgets.Add(CloseButton);

            Children.Add(TitlePanel);

            SetStyle(styleName);
        }

        public void ApplyWindowStyle(WindowStyle style)
        {
            ApplyWidgetStyle(style);

            if (style.TitleStyle != null)
            {
                _titleLabel.ApplyLabelStyle(style.TitleStyle);
            }

            if (style.CloseButtonStyle != null)
            {
                CloseButton.ApplyButtonStyle(style.CloseButtonStyle);
                if (style.CloseButtonStyle.ImageStyle != null)
                {
                    var image = (Image)CloseButton.Content;
                    image.ApplyPressableImageStyle(style.CloseButtonStyle.ImageStyle);
                }
            }
        }

        protected override void InternalSetStyle(Stylesheet stylesheet, string name)
        {
            ApplyWindowStyle(stylesheet.WindowStyles.SafelyGetStyle(name));
        }

        protected internal override void CopyFrom(Widget w)
        {
            base.CopyFrom(w);

            var window = (Window)w;

            Title = window.Title;
            TitleTextColor = window.TitleTextColor;
            TitleFont = window.TitleFont;
            CloseKey = window.CloseKey;
        }
    }
}
