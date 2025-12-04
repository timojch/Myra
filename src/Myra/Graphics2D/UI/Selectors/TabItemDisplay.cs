using Myra.Graphics2D.UI.Styles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Myra.Graphics2D.UI.Selectors;

public class TabItemDisplay : ListViewButton
{
    public Image Image { get; }

    public Label Label { get; }

    public Panel Panel { get => (Panel)this.Content; }

    public TabItemDisplay(TabItem item, Style<TabItemDisplay> style)
    {
        Image = new Image
        {
            Renderable = item.Image
        };

        Label = new Label(null)
        {
            Text = item.Text,
        };

        var panel = new HorizontalStackPanel
        {
            Spacing = item.ImageTextSpacing,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        panel.Widgets.Add(Image);
        panel.Widgets.Add(Label);

        this.SetStyle(style);

        HorizontalAlignment = HorizontalAlignment.Stretch;
		VerticalAlignment = VerticalAlignment.Stretch;
		Height = item.Height;
		Content = panel;
    }
}
