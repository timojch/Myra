#if MONOGAME || FNA
using Microsoft.Xna.Framework;
#elif STRIDE
using Stride.Core.Mathematics;
#else
using Color = FontStashSharp.FSColor;
#endif

namespace Myra.Graphics2D.UI.Styles
{
    public class FlowContainerStyle : WidgetStyle
    {
        public int IndentSize;

        public int LineSpacing;

        public int HorizontalSpacing;

        public int MinLineHeight;

        public int MaxLineHeight;

        public bool Wrap;

        public FlowContainerStyle()
        {
        }

        public FlowContainerStyle(FlowContainerStyle style) : base(style)
        {
            IndentSize = style.IndentSize;
            LineSpacing = style.LineSpacing;
            HorizontalSpacing = style.HorizontalSpacing;
            MinLineHeight = style.MinLineHeight;
            MaxLineHeight = style.MaxLineHeight;
            Wrap = style.Wrap;
        }

        public override WidgetStyle Clone()
        {
            return new FlowContainerStyle(this);
        }
    }
}
