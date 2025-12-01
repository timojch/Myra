using Myra.Graphics2D.UI.Styles;

namespace Myra.Graphics2D.UI
{
	public class HorizontalSplitPane : SplitPane
	{
		public override Orientation Orientation => Orientation.Horizontal;

		public HorizontalSplitPane(string styleName = Stylesheet.DefaultStyleName) : base(styleName)
		{
		}
	}
}