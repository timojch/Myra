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
    private TreeView _treeView;
    private TreeViewNode _upperNode;
    private TreeViewNode _lowerNode;
    private ContentPanel _content;
    private Label _titleLabel;

    public override Widget Content
    {
        get => _content.Content;
        set => _content.Content = value;
    }

    public bool IsExpanded
    {
        get => _upperNode.IsExpanded;
        set => _upperNode.IsExpanded = value;
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

    public Widget TitleBar
    {
        get => _upperNode;
    }

    public CollapseableFrame(string styleName = Stylesheet.DefaultStyleName)
    {
        _treeView = new TreeView(styleName);
        _titleLabel = new Label(styleName);
        _content = new ContentPanel();
        _upperNode = _treeView.AddSubNode(_titleLabel);
        _lowerNode = _upperNode.AddSubNode(_content);

        var layout = new SingleItemLayout<TreeView>(this);
        this.ChildrenLayout = layout;
        layout.Child = this._treeView;

        SetStyle(styleName);
    }

    protected override void InternalSetStyle(Stylesheet stylesheet, string name)
    {
        base.InternalSetStyle(stylesheet, name);
        _treeView.SetStyle(stylesheet, name);
        _titleLabel.SetStyle(stylesheet, name);
    }
}
