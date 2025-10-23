using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI.Styles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Myra.Graphics2D.UI.Misc;
public class CollapseableFrame : ContentControl
{
    private ContentPanel _content;
    private Image _expandToggle;
    private Widget _titleBar;
    private Label _titleLabel;

    public override Widget Content
    {
        get => _content.Content;
        set => _content.Content = value;
    }

    public bool IsExpanded
    {
        get;
        set
        {
            field = value;
            UpdateContentVisible();
        }
    }

    public string Title
    {
        get => _titleLabel.Text;
        set => _titleLabel.Text = value;
    }

    public Color TitleColor
    {
        get => _titleLabel.TextColor;
        set => _titleLabel.TextColor = value;
    }

    public Color? TitleOverColor
    {
        get => _titleLabel.OverTextColor;
        set => _titleLabel.OverTextColor = value;
    }

    public int SpacerWidth
    {
        get => _content.Padding.Left;
        set => _content.Padding = new Thickness(value, 0, 0, 0);
    }

    public IBrush TitleBackground
    {
        get => _titleBar.Background;
        set => _titleBar.Background = value;
    }

    public IBrush TitleOverBackground
    {
        get => _titleBar.OverBackground;
        set => _titleBar.OverBackground = value;
    }

    public CollapseableFrame(string styleName = Stylesheet.DefaultStyleName)
    {
        var stack = new VerticalStackPanel();
        var upperStack = new HorizontalStackPanel();

        _titleBar = upperStack;

        _expandToggle = new Image();
        _titleLabel = new Label(styleName);

        _content = new ContentPanel();

        stack.HorizontalAlignment = HorizontalAlignment.Stretch;
        upperStack.HorizontalAlignment = HorizontalAlignment.Stretch;
        StackPanel.SetProportionType(_titleLabel, ProportionType.Fill);
        StackPanel.SetProportionType(_content, ProportionType.Fill);

        SetStyle(styleName);

        _titleLabel.SingleLine = true;

        upperStack.Widgets.Add(_expandToggle);
        upperStack.Widgets.Add(_titleLabel);

        stack.Widgets.Add(upperStack);
        stack.Widgets.Add(_content);

        var layout = new SingleItemLayout<VerticalStackPanel>(this);
        this.ChildrenLayout = layout;
        layout.Child = stack;

        UpdateContentVisible();
        upperStack.MouseClick += (s, ev) =>
        {
            this.ToggleExpand();
        };
    }

    protected override void InternalSetStyle(Stylesheet stylesheet, string name)
    {
        base.InternalSetStyle(stylesheet, name);
        _titleLabel.SetStyle(stylesheet, name);
    }

    private void ToggleExpand()
    {
        this.IsExpanded = !this.IsExpanded;
    }

    private void UpdateContentVisible()
    {
        this._content.Visible = this.IsExpanded;
    }
}
