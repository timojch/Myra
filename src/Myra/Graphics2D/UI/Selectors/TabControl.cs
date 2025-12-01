using Myra.Attributes;
using Myra.Graphics2D.UI.Selectors;
using Myra.Graphics2D.UI.Styles;
using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Myra.Graphics2D.UI
{
    public enum TabSelectorPosition
    {
        Top,
        Right,
        Bottom,
        Left
    }

    public class TabControl : Selector<Grid, TabItem>
    {
        private Grid _gridButtons;
        private Panel _panelContent;
        private TabSelectorPosition _selectorPosition;

        [Browsable(false)]
        [XmlIgnore]
        public GenericStyle<TabItemDisplay> TabItemStyle { get; set; }

        [Browsable(false)]
        [XmlIgnore]
        public GenericStyle<Button> CloseButtonStyle { get; set; }

        [Browsable(false)]
        [XmlIgnore]
        public override SelectionMode SelectionMode { get => base.SelectionMode; set => base.SelectionMode = value; }

        [Category("Layout")]
        public int ButtonSpacing
        {
            get => _gridButtons.ColumnSpacing;
            set => _gridButtons.ColumnSpacing = value;

        }

        [Category("Layout")]
        public int HeaderSpacing
        {
            get => InternalChild.RowSpacing;
            set => InternalChild.RowSpacing = value;
        }

        [Content]
        [DefaultValue(null)]
        public Widget Content { get => _panelContent; }

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

        [Category("Behavior")]
        [DefaultValue(TabSelectorPosition.Top)]
        public TabSelectorPosition TabSelectorPosition
        {
            get
            {
                return _selectorPosition;
            }
            set
            {
                if (value == _selectorPosition)
                {
                    return;
                }

                _selectorPosition = value;
                UpdateSelectorPosition();
            }
        }

        [Category("Behavior")]
        [DefaultValue(false)]
        public bool CloseableTabs { get; set; }

        [DefaultValue(true)]
        public override bool ClipToBounds { get => base.ClipToBounds; set => base.ClipToBounds = value; }

        public TabControl(string styleName = Stylesheet.DefaultStyleName) : base(new Grid())
        {
            HorizontalAlignment = HorizontalAlignment.Left;
            VerticalAlignment = VerticalAlignment.Top;

            _gridButtons = new Grid();
            _panelContent = new Panel();

            _selectorPosition = TabSelectorPosition.Top;
            _gridButtons.DefaultColumnProportion = Proportion.Auto;
            _gridButtons.DefaultRowProportion = Proportion.Auto;

            InternalChild.DefaultColumnProportion = Proportion.Fill;
            InternalChild.DefaultRowProportion = Proportion.Fill;

            InternalChild.Widgets.Add(_gridButtons);
            InternalChild.Widgets.Add(_panelContent);

            UpdateSelectorPosition();

            ClipToBounds = true;

            SetStyle(styleName);
        }

        private void ItemOnChanged(object sender, EventArgs eventArgs)
        {
            var item = (TabItem)sender;

            var label = item.Display.Label;
            label.Text = item.Text;
            item.Display.SetStyle(TabItemStyle);

            if (SelectedItem == item)
            {
                UpdateContent();
            }

            InvalidateMeasure();
        }

        private void UpdateSelectorPosition()
        {
            switch (_selectorPosition)
            {
                case TabSelectorPosition.Top:
                    Grid.SetColumn(_gridButtons, 0);
                    Grid.SetRow(_gridButtons, 0);

                    Grid.SetColumn(_panelContent, 0);
                    Grid.SetRow(_panelContent, 1);

                    InternalChild.ColumnsProportions.Clear();
                    InternalChild.RowsProportions.Clear();
                    InternalChild.RowsProportions.Add(Proportion.Auto);
                    InternalChild.RowsProportions.Add(Proportion.Fill);
                    break;

                case TabSelectorPosition.Right:
                    Grid.SetColumn(_gridButtons, 1);
                    Grid.SetRow(_gridButtons, 0);

                    Grid.SetColumn(_panelContent, 0);
                    Grid.SetRow(_panelContent, 0);

                    InternalChild.ColumnsProportions.Clear();
                    InternalChild.ColumnsProportions.Add(Proportion.Fill);
                    InternalChild.ColumnsProportions.Add(Proportion.Auto);
                    InternalChild.RowsProportions.Clear();
                    break;

                case TabSelectorPosition.Bottom:
                    Grid.SetColumn(_gridButtons, 0);
                    Grid.SetRow(_gridButtons, 1);

                    Grid.SetColumn(_panelContent, 0);
                    Grid.SetRow(_panelContent, 0);


                    InternalChild.ColumnsProportions.Clear();
                    InternalChild.RowsProportions.Clear();
                    InternalChild.RowsProportions.Add(Proportion.Fill);
                    InternalChild.RowsProportions.Add(Proportion.Auto);
                    break;

                case TabSelectorPosition.Left:
                    Grid.SetColumn(_gridButtons, 0);
                    Grid.SetRow(_gridButtons, 0);

                    Grid.SetColumn(_panelContent, 1);
                    Grid.SetRow(_panelContent, 0);

                    InternalChild.ColumnsProportions.Clear();
                    InternalChild.ColumnsProportions.Add(Proportion.Auto);
                    InternalChild.ColumnsProportions.Add(Proportion.Fill);
                    InternalChild.RowsProportions.Clear();
                    break;
            }

            UpdateButtonsGrid();
        }

        private void UpdateButtonsGrid()
        {
            bool tabSelectorIsLeftOrRight = TabSelectorPosition == TabSelectorPosition.Left ||
                                            TabSelectorPosition == TabSelectorPosition.Right;
            for (var i = 0; i < _gridButtons.Widgets.Count; ++i)
            {
                var widget = _gridButtons.Widgets[i];
                if (tabSelectorIsLeftOrRight)
                {
                    Grid.SetColumn(widget, 0);
                    Grid.SetRow(widget, i);
                }
                else
                {
                    Grid.SetColumn(widget, i);
                    Grid.SetRow(widget, 0);
                }
            }
        }

        protected override void InsertItem(TabItem item, int index)
        {
            item.Changed += ItemOnChanged;

            var display = new TabItemDisplay(item, TabItemStyle);
            display.ButtonsContainer = _gridButtons;

            display.Click += ButtonOnClick;

            item.Display = display;

            if (!CloseableTabs)
            {
                display.Tag = item;
                _gridButtons.Widgets.Insert(index, display);
            }
            else
            {
                var topItemPanel = new HorizontalStackPanel();
                topItemPanel.Tag = item;

                topItemPanel.Widgets.Add(display);
                StackPanel.SetProportionType(display, ProportionType.Fill);

                var closeButton = new Button
                {
                    Content = new Image(),
                    HorizontalAlignment = HorizontalAlignment.Right
                };

                closeButton.Click += (s, e) => Items.Remove(item);

                var style = TabItemStyle;
                if (CloseButtonStyle != null)
                {
                    closeButton.SetStyle(CloseButtonStyle);
                }

                topItemPanel.Widgets.Add(closeButton);
                _gridButtons.Widgets.Insert(index, topItemPanel);
            }

            UpdateButtonsGrid();

            if (Items.Count == 1)
            {
                // Select first item
                SelectedItem = item;
            }
        }

        private int GetButtonIndex(ListViewButton button)
        {
            var index = -1;
            for (var i = 0; i < _gridButtons.Widgets.Count; ++i)
            {
                var widget = _gridButtons.Widgets[i];
                if (widget == button || widget.FindChild<ListViewButton>() == button)
                {
                    index = i;
                    break;
                }
            }

            return index;
        }

        protected override void RemoveItem(TabItem item)
        {
            item.Changed -= ItemOnChanged;

            var index = GetButtonIndex(item.Display);
            if (index < 0)
            {
                return;
            }

            _gridButtons.Widgets.RemoveAt(index);
            if (SelectedItem == item)
            {
                SelectedItem = null;
            }

            UpdateButtonsGrid();
        }

        private void UpdateContent()
        {
            _panelContent.Widgets.Clear();
            if (SelectedItem != null && SelectedItem.Content != null)
            {
                _panelContent.Widgets.Add(SelectedItem.Content);
            }
        }

        protected override void OnSelectedItemChanged()
        {
            base.OnSelectedItemChanged();
            UpdateContent();
        }

        protected override void Reset()
        {
            while (_gridButtons.Widgets.Count > 0)
            {
                RemoveItem((TabItem)_gridButtons.Widgets[0].Tag);
            }
        }

        private void ButtonOnClick(object sender, EventArgs eventArgs)
        {
            var button = (ListViewButton)sender;
            var index = GetButtonIndex(button);
            if (index < 0)
            {
                return;
            }

            SelectedIndex = index;
        }

        protected internal override void CopyFrom(Widget w)
        {
            base.CopyFrom(w);

            var tabControl = (TabControl)w;

            TabItemStyle = tabControl.TabItemStyle;
            TabSelectorPosition = tabControl.TabSelectorPosition;

            foreach (var item in tabControl.Items)
            {
                Items.Add(item.Clone());
            }
        }
    }
}