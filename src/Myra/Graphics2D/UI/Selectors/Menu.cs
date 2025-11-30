using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Myra.Graphics2D.UI.Styles;
using Myra.Utility;
using System.Xml.Serialization;
using Myra.Attributes;
using FontStashSharp;
using Myra.Events;
using Myra.Graphics2D.UI.Frames;
using info.lundin.math;




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
    public abstract class Menu : Widget
    {
        private readonly SingleItemLayout<Grid> _layout;
        private bool _dirty = true;
        private bool _internalSetSelectedIndex = false;
        private Point? _offsetFromParentMenu;

        [Browsable(false)]
        [XmlIgnore]
        public abstract Orientation Orientation { get; }

        [Browsable(false)]
        [XmlIgnore]
        internal MenuItem OpenMenuItem { get; private set; }

        [Browsable(false)]
        [XmlIgnore]
        public bool IsOpen
        {
            get
            {
                return OpenMenuItem != null;
            }
        }

        [Browsable(false)]
        [Content]
        public ObservableCollection<IMenuItem> Items { get; } = new ObservableCollection<IMenuItem>();

        [Browsable(false)]
        public bool IsSubMenu { get => this.ParentMenu is not null; }

        [Browsable(false)]
        public Menu ParentMenu { get; set; }

        [Category("Style")]
        public GenericStyle<Label> LabelStyle { get; set; }

        [Category("Style")]
        public GenericStyle<Label> ShortcutStyle { get; set; }

        [Category("Style")]
        public GenericStyle<Image> ImageStyle { get; set; }

        [Category("Style")]
        public GenericStyle<SeparatorWidget> SeparatorStyle { get; set; }

        [Category("Appearance")]
        public SpriteFontBase LabelFont
        {
            get
            {
                return LabelStyle.GetAttribute<SpriteFontBase>("Font");
            }

            set
            {
                LabelStyle.AddAttribute("Font", value);
            }
        }

        [Category("Appearance")]
        [StylePropertyPath("/LabelStyle/TextColor")]
        public Color LabelColor
        {
            get
            {
                return LabelStyle.GetAttribute<Color>("TextColor");
            }

            set
            {
                LabelStyle.AddAttribute("TextColot", value);
            }
        }

        [Category("Appearance")]
        [StylePropertyPath("/LabelStyle/SpecialCharColor")]
        public Color? SpecialCharColor { get; set; }

        [Category("Appearance")]
        public IBrush SelectionHoverBackground
        {
            get
            {
                return InternalChild.SelectionHoverBackground;
            }

            set
            {
                InternalChild.SelectionHoverBackground = value;
            }
        }

        [Category("Appearance")]
        public IBrush SelectionBackground
        {
            get
            {
                return InternalChild.SelectionBackground;
            }

            set
            {
                InternalChild.SelectionBackground = value;
            }
        }

        [Category("Appearance")]
        [DefaultValue(HorizontalAlignment.Left)]
        public HorizontalAlignment LabelHorizontalAlignment { get; set; }

        [Category("Behavior")]
        [DefaultValue(true)]
        public bool HoverIndexCanBeNull
        {
            get
            {
                return InternalChild.HoverIndexCanBeNull;
            }

            set
            {
                InternalChild.HoverIndexCanBeNull = value;
            }
        }

        public override Desktop Desktop
        {
            get
            {
                return base.Desktop;
            }

            internal set
            {
                if (Desktop != null)
                {
                    Desktop.ContextMenuClosed -= DesktopOnContextMenuClosed;

                    if (Desktop.ContextMenu == this)
                    {
                        this.Closed?.Invoke(this, EventArgs.Empty);
                    }
                }

                base.Desktop = value;

                if (Desktop != null)
                {
                    Desktop.ContextMenuClosed += DesktopOnContextMenuClosed;
                }
            }
        }

        [Browsable(false)]
        [XmlIgnore]
        public int? HoverIndex
        {
            get
            {
                if (Orientation == Orientation.Horizontal)
                {
                    return InternalChild.HoverColumnIndex;
                }

                return InternalChild.HoverRowIndex;
            }

            set
            {

                if (Orientation == Orientation.Horizontal)
                {
                    InternalChild.HoverColumnIndex = value;
                }
                else
                {
                    InternalChild.HoverRowIndex = value;
                }
            }
        }

        [Browsable(false)]
        [XmlIgnore]
        public int? SelectedIndex
        {
            get
            {
                if (Orientation == Orientation.Horizontal)
                {
                    return InternalChild.SelectedColumnIndex;
                }

                return InternalChild.SelectedRowIndex;
            }

            set
            {

                if (Orientation == Orientation.Horizontal)
                {
                    InternalChild.SelectedColumnIndex = value;
                }
                else
                {
                    InternalChild.SelectedRowIndex = value;
                }
            }
        }

        public event EventHandler Closed;

        private IMenuItem SelectedMenuItem
        {
            get
            {
                return GetMenuItem(SelectedIndex);
            }
        }

        protected Grid InternalChild => _layout.Child;

        protected Menu(string styleName)
        {
            _layout = new SingleItemLayout<Grid>(this)
            {
                Child = new Grid
                {
                    CanSelectNothing = false
                }
            };
            ChildrenLayout = _layout;

            Items.CollectionChanged += ItemsOnCollectionChanged;

            AcceptsKeyboardFocus = true;

            if (Orientation == Orientation.Horizontal)
            {
                InternalChild.GridSelectionMode = GridSelectionMode.Column;
                InternalChild.DefaultColumnProportion = Proportion.Auto;
                InternalChild.DefaultRowProportion = Proportion.Auto;
            }
            else
            {
                InternalChild.GridSelectionMode = GridSelectionMode.Row;
                InternalChild.ColumnsProportions.Add(Proportion.Fill);
                InternalChild.DefaultRowProportion = Proportion.Auto;
            }

            InternalChild.HoverIndexChanged += OnHoverIndexChanged;
            InternalChild.SelectedIndexChanged += OnSelectedIndexChanged;
            InternalChild.TouchUp += InternalChild_TouchUp;

            OpenMenuItem = null;

            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
            HoverIndexCanBeNull = true;

            AfterRender = (c) => UpdatePosition();

            SetStyle(styleName);
        }

        private void ItemsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            if (args.Action == NotifyCollectionChangedAction.Add)
            {
                var index = args.NewStartingIndex;
                foreach (IMenuItem item in args.NewItems)
                {
                    InsertItem(item, index);
                    ++index;
                }
            }
            else if (args.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (IMenuItem item in args.OldItems)
                {
                    RemoveItem(item);
                }
            }
            else if (args.Action == NotifyCollectionChangedAction.Reset)
            {
                InternalChild.Widgets.Clear();
            }

            _dirty = true;
        }

        /// <summary>
        /// Recursively search for the menu item by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns>null if not found</returns>
        public MenuItem FindMenuItemById(string id)
        {
            foreach (var item in Items)
            {
                var asMenuItem = item as MenuItem;
                if (asMenuItem == null)
                {
                    continue;
                }

                var result = asMenuItem.FindMenuItemById(id);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        protected virtual void UpdateWidgets()
        {
        }

        protected override void InternalArrange()
        {
            base.InternalArrange();
            if (this.ParentMenu is not null)
            {
                _offsetFromParentMenu = this.ParentMenu.ToLocal(this.ToGlobal(Point.Zero));
            }
            else
            {
                _offsetFromParentMenu = null;
            }
        }

        private void SetMenuItem(MenuItem menuItem)
        {
            menuItem.ImageWidget.Renderable = menuItem.Image;
            if (menuItem.ImageWidget.Renderable != null && !InternalChild.Widgets.Contains(menuItem.ImageWidget))
            {
                InternalChild.Widgets.Add(menuItem.ImageWidget);
            }
            else if (menuItem.ImageWidget.Renderable == null && InternalChild.Widgets.Contains(menuItem.ImageWidget))
            {
                InternalChild.Widgets.Remove(menuItem.ImageWidget);
            }

            menuItem.Shortcut.Text = menuItem.ShortcutText;
            if (menuItem.ShortcutColor != null)
            {
                menuItem.Shortcut.TextColor = menuItem.ShortcutColor.Value;
            }
            else if (ShortcutStyle != null)
            {
                ShortcutStyle.ApplyTo(menuItem.Shortcut);
            }

            if (!string.IsNullOrEmpty(menuItem.ShortcutText) && !InternalChild.Widgets.Contains(menuItem.Shortcut))
            {
                InternalChild.Widgets.Add(menuItem.Shortcut);
            }
            else if (string.IsNullOrEmpty(menuItem.ShortcutText) && InternalChild.Widgets.Contains(menuItem.Shortcut))
            {
                InternalChild.Widgets.Remove(menuItem.Shortcut);
            }

            menuItem.Label.Text = menuItem.DisplayText;
            if (menuItem.Color != null)
            {
                menuItem.Label.TextColor = menuItem.Color.Value;
            }
            else if (LabelStyle != null)
            {
                LabelStyle.ApplyTo(menuItem.Label);
            }

            menuItem.Label.HorizontalAlignment = LabelHorizontalAlignment;

            UpdateWidgets();
        }

        private void SetAdvancedMenuItem(AdvancedMenuItem menuItem)
        {
            if (menuItem.LeftMargin is not null && !InternalChild.Widgets.Contains(menuItem.LeftMargin))
            {
                InternalChild.Widgets.Add(menuItem.LeftContentPanel);
            }
            else if (menuItem.LeftMargin is null && InternalChild.Widgets.Contains(menuItem.LeftMargin))
            {
                InternalChild.Widgets.Remove(menuItem.LeftContentPanel);
            }

            if (menuItem.RightMargin is not null && !InternalChild.Widgets.Contains(menuItem.RightMargin))
            {
                InternalChild.Widgets.Add(menuItem.RightContentPanel);
            }
            else if (menuItem.LeftMargin is null && InternalChild.Widgets.Contains(menuItem.RightMargin))
            {
                InternalChild.Widgets.Remove(menuItem.RightContentPanel);
            }

            UpdateWidgets();
        }

        private void MenuItemOnChanged(object sender, EventArgs eventArgs)
        {
            if (sender is MenuItem menuItem)
            {
                SetMenuItem(menuItem);
            }
            else if (sender is AdvancedMenuItem advancedMenuItem)
            {
                SetAdvancedMenuItem(advancedMenuItem);
            }
        }

        private void InsertItem(IMenuItem item, int index)
        {
            item.Menu = this;

            if (item is MenuItem menuItem)
            {
                menuItem.Changed += MenuItemOnChanged;

                if (Orientation == Orientation.Horizontal)
                {
                    LabelStyle.ApplyTo(menuItem.Label);
                }
                else
                {
                    ImageStyle.ApplyTo(menuItem.ImageWidget);
                    LabelStyle.ApplyTo(menuItem.Label);
                    ShortcutStyle.ApplyTo(menuItem.Shortcut);
                }

                // Add only label, as other widgets(image and shortcut) would be optionally added by SetMenuItem
                InternalChild.Widgets.Add(menuItem.Label);
                SetMenuItem(menuItem);
            }
            else if (item is AdvancedMenuItem advancedItem)
            {
                advancedItem.Changed += MenuItemOnChanged;

                InternalChild.Widgets.Add(advancedItem.BodyContentPanel);
                SetAdvancedMenuItem(advancedItem);
            }
            else if (item is MenuSeparator separatorItem)
            {
                var separator = separatorItem.Separator;
                if (Orientation == Orientation.Horizontal)
                {
                    separator = new VerticalSeparator(null);
                }
                else
                {
                    separator = new HorizontalSeparator(null);
                }

                SeparatorStyle.ApplyTo(separator);

                InternalChild.Widgets.Add(separator);

                ((MenuSeparator)item).Separator = separator;
            }
        }

        private void RemoveItem(IMenuItem item)
        {
            var menuItem = item as MenuItem;
            if (menuItem != null)
            {
                menuItem.Changed -= MenuItemOnChanged;
                InternalChild.Widgets.Remove(menuItem.ImageWidget);
                InternalChild.Widgets.Remove(menuItem.Label);
                InternalChild.Widgets.Remove(menuItem.Shortcut);
            }
            else
            {
                InternalChild.Widgets.Remove(((MenuSeparator)item).Separator);
            }
        }

        public void Close()
        {
            if (Desktop != null)
            {
                Desktop.HideContextMenu();
            }
            HoverIndex = SelectedIndex = null;
        }

        private Rectangle GetItemBounds(int index)
        {
            var bounds = InternalChild.Bounds;
            if (Orientation == Orientation.Horizontal)
            {
                return new Rectangle(bounds.X + InternalChild.GetCellLocationX(index),
                    bounds.Y,
                    InternalChild.GetColumnWidth(index),
                    bounds.Height);
            }

            return new Rectangle(bounds.X,
                bounds.Y + InternalChild.GetCellLocationY(index),
                bounds.Width,
                InternalChild.GetRowHeight(index));
        }

        private void DesktopOnContextMenuClosed(object sender, GenericEventArgs<Widget> genericEventArgs)
        {
            OpenMenuItem = null;

            if (!_internalSetSelectedIndex)
            {
                SelectedIndex = HoverIndex = null;
            }

            this.Closed?.Invoke(this, EventArgs.Empty);
        }

        private void OnHoverIndexChanged(object sender, EventArgs eventArgs)
        {
            var menuItem = GetMenuItem(HoverIndex);
            if ((menuItem is null || !menuItem.CanInteract) && HoverIndexCanBeNull)
            {
                // Separators couldn't be selected
                HoverIndex = null;
                return;
            }

            if (!IsOpen)
            {
                return;
            }

            if (menuItem is MenuItem maybeSubmenu)
            {
                if (Desktop.ContextMenu != this && maybeSubmenu.CanOpen && OpenMenuItem != maybeSubmenu)
                {
                    SelectedIndex = HoverIndex;
                }
            }
        }

        private void ShowSubMenu(MenuItem menuItem)
        {
            var bounds = GetItemBounds(menuItem.Index);

            var pos = this is HorizontalMenu ? new Point(bounds.X, bounds.Bottom) : new Point(bounds.Right, bounds.Y);
            pos = ToGlobal(pos);

            if (Desktop is not null)
            {
                Desktop.ShowContextMenu(menuItem.SubMenu, pos);
                OpenMenuItem = menuItem;
            }
        }

        private void OnSelectedIndexChanged(object sender, EventArgs e)
        {
            if (OpenMenuItem != null)
            {
                try
                {
                    _internalSetSelectedIndex = true;

                    if (Desktop.ContextMenu != this)
                    {
                        Desktop.HideContextMenu();
                    }
                }
                finally
                {
                    _internalSetSelectedIndex = false;
                }
            }

            var menuItem = SelectedMenuItem as MenuItem;
            if (menuItem != null && menuItem.CanOpen)
            {
                ShowSubMenu(menuItem);
            }
        }

        private void InternalChild_TouchUp(object sender, EventArgs e)
        {
            var menuItem = SelectedMenuItem;
            if (menuItem != null)
            {
                menuItem.Invoke();
                if (menuItem.CloseAfterInvoke)
                {
                    Close();
                }
            }
        }

        private IMenuItem GetMenuItem(int? index)
        {
            if (index == null)
            {
                return null;
            }

            return Items[index.Value];
        }

        private void Click(int? index)
        {
            var menuItem = GetMenuItem(index);
            if (menuItem == null)
            {
                return;
            }

            menuItem.Invoke();
            if (menuItem.CloseAfterInvoke)
            {
                Close();
            }
            else
            {
                SelectedIndex = HoverIndex = index;
            }
        }

        public override void OnKeyDown(Keys k)
        {
            if (k == Keys.Enter || k == Keys.Space)
            {
                int? selectedIndex = HoverIndex;
                if (selectedIndex != null)
                {
                    var menuItem = Items[selectedIndex.Value] as MenuItem;
                    if (menuItem != null && !menuItem.CanOpen)
                    {
                        Click(menuItem.Index);
                        return;
                    }
                }
            }

            var ch = k.ToChar(false);
            if (ch != null)
            {
                var c = char.ToLower(ch.Value);
                foreach (var w in Items)
                {
                    var menuItem = w as MenuItem;
                    if (menuItem == null)
                    {
                        continue;
                    }

                    if (menuItem.UnderscoreChar == c && !menuItem.IsOpen)
                    {
                        Click(menuItem.Index);
                        return;
                    }
                }
            }

            if (OpenMenuItem != null)
            {
                OpenMenuItem.SubMenu.OnKeyDown(k);
            }
        }

        public void MoveHover(int delta)
        {
            if (Items.Count == 0)
            {
                return;
            }

            // First step - determine index of currently selected item
            var si = SelectedIndex;
            if (si == null)
            {
                si = HoverIndex;
            }
            var hoverIndex = si != null ? si.Value : -1;
            var oldHover = hoverIndex;

            var iterations = 0;
            while (true)
            {
                if (iterations > Items.Count)
                {
                    return;
                }

                hoverIndex += delta;

                if (hoverIndex < 0)
                {
                    hoverIndex = Items.Count - 1;
                }

                if (hoverIndex >= Items.Count)
                {
                    hoverIndex = 0;
                }

                if (Items[hoverIndex].CanInteract)
                {
                    break;
                }

                ++iterations;
            }

            var menuItem = Items[hoverIndex] as MenuItem;
            if (menuItem != null)
            {
                HoverIndex = menuItem.Index;
            }
        }

        protected abstract void PlaceMenuItemInGrid(IMenuItem item, int index);

        private void UpdateGrid()
        {
            if (!_dirty)
            {
                return;
            }

            var index = 0;

            foreach (var item in Items)
            {
                PlaceMenuItemInGrid(item, index);
                ++index;
            }

            _dirty = false;
        }

        protected override Point InternalMeasure(Point availableSize)
        {
            UpdateGrid();
            return base.InternalMeasure(availableSize);
        }

        public void InvalidateMenuContent()
        {
            _dirty = true;
        }

        public void ApplyMenuStyle(MenuStyle style)
        {
            var clone = new MenuStyle(style);

            ApplyWidgetStyle(clone);

            InternalChild.SelectionHoverBackground = style.SelectionHoverBackground;
            InternalChild.SelectionBackground = style.SelectionBackground;
        }

        private void UpdatePosition()
        {
            if (_offsetFromParentMenu.HasValue && ParentMenu is not null)
            {
                var targetPosition = ParentMenu.ToGlobal(_offsetFromParentMenu.Value);
                this.Left = targetPosition.X;
                this.Top = targetPosition.Y;
            }
        }
    }
}