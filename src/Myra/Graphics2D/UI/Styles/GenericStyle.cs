using FontStashSharp;
using FontStashSharp.RichText;
using Microsoft.Xna.Framework;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.TextureAtlases;
using Myra.MML;
using Myra.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Myra.Graphics2D.UI.Styles;

public abstract class GenericStyle
{
    public abstract void AddSubWidgetStyle(string propertyName, IStyle style);

    public abstract Type GetPropertyType(string propertyName);

    public abstract void AddAttribute(string propertyName, object value);
}

public class GenericStyle<TWidget>
    : GenericStyle, IStyle<TWidget>
    where TWidget : Widget
{
    private Dictionary<PropertyInfo, object> ValuePairs = new();

    private Dictionary<PropertyInfo, IStyle> StylePairs = new();

    private const BindingFlags PropertyBindingFlags =
        BindingFlags.Public |
        BindingFlags.Instance |
        BindingFlags.FlattenHierarchy;
    private const BindingFlags SubWidgetBindingFlags =
        BindingFlags.Public | BindingFlags.NonPublic |
        BindingFlags.Instance |
        BindingFlags.FlattenHierarchy;

    public string TypeName { get => typeof(TWidget).Name; }

    public GenericStyle()
    {
    }

    public override void AddSubWidgetStyle(string propertyName, IStyle style)
    {
        var propertyInfo = typeof(TWidget).GetProperty(propertyName, GenericStyle<TWidget>.SubWidgetBindingFlags);
        if (propertyInfo is null)
        {
            throw new InvalidDataException($"No property named {propertyName} could be found in {this.TypeName}.");
        }

        this.StylePairs[propertyInfo] = style;
    }

    public override void AddAttribute(string propertyName, object value)
    {
        var propertyInfo = typeof(TWidget).GetProperty(propertyName, GenericStyle<TWidget>.PropertyBindingFlags);
        if (propertyInfo is null)
        {
            throw new InvalidDataException($"No property named {propertyName} could be found in {this.TypeName}.");
        }
        else if (!this.IsStyleableProperty(propertyInfo))
        {
            throw new InvalidDataException($"The property named {propertyName} in {this.TypeName} is not styleable.");
        }

        if (value is null)
        {
            if (!propertyInfo.PropertyType.IsValueType)
            {
                this.ValuePairs[propertyInfo] = value;
            }
            else
            {
                throw new InvalidDataException($"A null value cannot be assigned into the property {propertyName} in {this.TypeName}");
            }
        }
        else if (value.GetType().IsAssignableTo(propertyInfo.PropertyType))
        {
            this.ValuePairs[propertyInfo] = value;
        }
        else
        {
            throw new InvalidDataException($"A value of type {value.GetType().Name} cannot be assigned into the property {propertyName} in {this.TypeName}");
        }
    }

    public override Type GetPropertyType(string propertyName)
    {
        var propertyInfo = typeof(TWidget).GetProperty(propertyName, GenericStyle<TWidget>.PropertyBindingFlags);
        if (propertyInfo is null)
        {
            throw new InvalidDataException($"No property named {propertyName} could be found in {this.TypeName}.");
        }

        return propertyInfo.PropertyType;
    }

    public void ApplyTo(TWidget widget)
    {
        foreach (var pair in this.ValuePairs)
        {
            pair.Key.SetValue(widget, pair.Value);
        }

        foreach (var pair in this.StylePairs)
        {
            var target = (Widget)pair.Key.GetValue(widget);
            pair.Value.ApplyTo(target);
        }
    }

    public GenericStyle<TWidget> Clone()
    {
        var clone = new GenericStyle<TWidget>();

        foreach (var valuePair in this.ValuePairs)
        {
            clone.AddAttribute(valuePair.Key.Name, valuePair.Value);
        }

        foreach (var stylePair in this.StylePairs)
        {
            clone.AddSubWidgetStyle(stylePair.Key.Name, stylePair.Value);
        }

        return clone;
    }

    private bool IsStyleableProperty(PropertyInfo propertyInfo)
    {
        var category = propertyInfo.GetCustomAttribute<CategoryAttribute>()?.Category;
        return category switch
        {
            "Appearance" => true,
            "Behavior" => true,
            "Grid" => true,
            "Layout" => true,
            "Transform" => true,
            "Debug" => false,
            null => false,
            _ => throw new NotSupportedException($"Category {category} is not defined as styleable."),
        };
    }
}
