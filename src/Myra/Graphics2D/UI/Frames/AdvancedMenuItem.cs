using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Xml.Serialization;
using System;
using Myra.Attributes;
using Myra.MML;
using FontStashSharp.RichText;

#if MONOGAME || FNA
using Microsoft.Xna.Framework;
#elif STRIDE
using Stride.Core.Mathematics;
#else
using Color = FontStashSharp.FSColor;
#endif

namespace Myra.Graphics2D.UI.Frames;
public class AdvancedMenuItem
    : BaseObject, IMenuItem
{
    public Menu Menu { get; set; }

    public int Index { get; set; }

    public Widget LeftMargin
    {
        get => this.LeftContentPanel.Content;
        set
        {
            if (this.LeftMargin != value)
            {
                this.LeftContentPanel.Content = value;
                this.FireChanged();
            }
        }
    }

    public Widget Body
    {
        get => this.BodyContentPanel.Content;
        set
        {
            if (this.Body != value)
            {
                this.BodyContentPanel.Content = value;
                this.FireChanged();
            }
        }
    }

    public Widget RightMargin
    {
        get => this.RightContentPanel.Content;
        set
        {
            if (this.RightMargin != value)
            {
                this.RightContentPanel.Content = value;
                this.FireChanged();
            }
        }
    }

    public event EventHandler Selected;
    public event EventHandler Changed;

    internal readonly ContentPanel LeftContentPanel = new();
    internal readonly ContentPanel BodyContentPanel = new();
    internal readonly ContentPanel RightContentPanel = new();

    public void FireSelected()
    {
        this.Selected?.Invoke(this, EventArgs.Empty);
    }

    public void FireChanged()
    {
        this.Changed?.Invoke(this, EventArgs.Empty);
    }
}
