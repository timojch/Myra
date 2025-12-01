using System;
using Myra.Graphics2D.UI.Styles;

#if MONOGAME || FNA
using Microsoft.Xna.Framework;
#elif STRIDE
using Stride.Core.Mathematics;
#else
using Color = FontStashSharp.FSColor;
#endif

namespace Myra.Graphics2D.UI
{
    public class TreeViewNode : ContentControl, ITreeViewNode
    {
        private readonly GridLayout _layout = new GridLayout();
        private readonly TreeView _topTree;
        private readonly VerticalStackPanel _childNodesStackPanel;
        private readonly ToggleButton _mark;
        private Widget _content;

        public bool IsExpanded
        {
            get { return _mark.IsPressed; }

            set { _mark.IsPressed = value; }
        }

		public int Depth
		{
			get
			{
				if (this.ParentNode is null)
				{
					return 0;
				}
				else
				{
					return this.ParentNode.Depth + 1;
				}
			}
		}

		public ToggleButton Mark => _mark;

        internal VerticalStackPanel ChildNodesGrid => _childNodesStackPanel;

        public int ContentHeight => _layout.GetRowHeight(0);

        public int ChildNodesCount => _childNodesStackPanel.Children.Count;

        internal bool RowVisible { get; set; }

		public TreeViewNode ParentNode { get; internal set; }

        public override Widget Content
        {
            get => _content;

            set
            {
                if (_content == value)
                {
                    return;
                }

                if (_content != null)
                {
                    Children.Remove(_content);
                }

                _content = value;

                if (_content != null)
                {
                    Grid.SetColumn(_content, 1);
                    Children.Add(_content);
                }
            }
        }

        internal TreeViewNode(TreeView topTree, IStyle<TreeViewNode> style)
        {
            _layout.ColumnSpacing = 2;
            _layout.RowSpacing = 2;
            ChildrenLayout = _layout;

            _topTree = topTree;

            if (_topTree != null)
            {
                _topTree.AllNodes.Add(this);
            }

			_mark = new ToggleButton(null)
			{
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Center,
				Content = new Image()
			};

            _mark.PressedChanged += (s, a) =>
            {
                _childNodesStackPanel.Visible = _mark.IsPressed;
            };

            Children.Add(_mark);

            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;

            _layout.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            _layout.ColumnsProportions.Add(new Proportion(ProportionType.Fill));

            _layout.RowsProportions.Add(new Proportion(ProportionType.Auto));
            _layout.RowsProportions.Add(new Proportion(ProportionType.Auto));

			// Second is yet another grid holding child nodes
			_childNodesStackPanel = new VerticalStackPanel
			{
				Visible = false,
			};
			Grid.SetColumn(_childNodesStackPanel, 1);
			Grid.SetRow(_childNodesStackPanel, 1);

            Children.Add(_childNodesStackPanel);

            style.ApplyTo(this);

            UpdateMark();
        }

        internal TreeViewNode(TreeView topTree, string styleName = Stylesheet.DefaultStyleName)
            : this(topTree, Stylesheet.Current.GetStyle<TreeViewNode>(styleName))
        {
        }

        private void MarkOnUp(object sender, EventArgs args)
        {
            _childNodesStackPanel.Visible = false;
        }

        protected virtual void UpdateMark()
        {
            _mark.Visible = _childNodesStackPanel.Children.Count > 0;
        }

        public virtual void RemoveAllSubNodes()
        {
            _childNodesStackPanel.Children.Clear();
            UpdateMark();
        }

        public TreeViewNode AddSubNode(Widget content)
        {
            var result = new TreeViewNode(_topTree, (IStyle<TreeViewNode>)this.StyledBy)
            {
                ParentNode = this,
                Content = content
            };
            Grid.SetRow(result, _childNodesStackPanel.Children.Count);

            _childNodesStackPanel.Children.Add(result);

            UpdateMark();

            return result;
        }

		public TreeViewNode GetSubNode(int index)
		{
			return (TreeViewNode)_childNodesStackPanel.Children[index];
		}

        public void RemoveSubNode(TreeViewNode subNode)
        {
            _childNodesStackPanel.Children.Remove(subNode);
            _topTree.AllNodes.Remove(subNode);
            if (_topTree != null)
            {
                if (_topTree.SelectedNode == subNode)
                {
                    _topTree.SelectedNode = null;
                }

                if (_topTree.HoverRow == subNode)
                {
                    _topTree.HoverRow = null;
                }
            }
        }

		public void RemoveSubNodeAt(int index)
		{
			var subNode = (TreeViewNode)_childNodesStackPanel.Children[index];
			_childNodesStackPanel.Children.RemoveAt(index);
			_topTree.AllNodes.Remove(subNode);
			if (_topTree.SelectedNode == subNode)
			{
				_topTree.SelectedNode = null;
			}
		}
	}
}