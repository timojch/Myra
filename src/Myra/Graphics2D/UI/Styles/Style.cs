using FontStashSharp;
using FontStashSharp.RichText;
using info.lundin.math;
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
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Myra.Graphics2D.UI.Styles;

public abstract class Style
{
    private const BindingFlags PropertyBindingFlags =
        BindingFlags.Public |
        BindingFlags.Instance |
        BindingFlags.FlattenHierarchy;

    public string Name { get; set; }

    public abstract bool CanHaveContent { get; }

    public abstract Type TargetType { get; }

    public abstract TValue GetAttribute<TValue>(string name);

    public abstract GenericStyle<TWidget> GetSubStyle<TWidget>(string name)
        where TWidget : Widget;

    public abstract void AddContentStyle(Type contentType, IStyle style);

    public abstract void AddSubWidgetStyle(string propertyOrContentTypeName, IStyle style);

    public abstract Type GetPropertyType(string propertyOrContentTypeName);

    public abstract void AddAttribute(string propertyName, object value);

    public abstract bool TryGetProperty(string propertyName, out PropertyInfo value);

    internal static Type FindWidgetType(string widgetName)
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes()
                .Where(t => t.Name.Equals(widgetName, StringComparison.OrdinalIgnoreCase))
                .Where(t => t.IsAssignableTo(typeof(Widget))))
            .FirstOrDefault();
    }

    internal static PropertyInfo FindProperty(Type targetType, string propertyName)
    {
        var propertyInfo = targetType.GetProperties(Style.PropertyBindingFlags)
            .Where(p => p.Name == propertyName)
            .Concat(targetType.GetProperties(Style.PropertyBindingFlags)
                .Where(p => p.GetCustomAttribute<AlsoKnownAsAttribute>()?.Name == propertyName))
            .FirstOrDefault();

        return propertyInfo;
    }
}

public class GenericStyle<TWidget>
    : Style, IStyle<TWidget>
    where TWidget : Widget
{

    private Dictionary<PropertyInfo, object> ValuePairs = new();

    private Dictionary<PropertyInfo, IStyle> StylePairs = new();

    private Type ContentType;

    private IStyle ContentStyle;

    public string TypeName { get => typeof(TWidget).Name; }

    public override Type TargetType => typeof(TWidget);

    public override bool CanHaveContent { get => typeof(TWidget).IsAssignableTo(typeof(ContentControl)); }

    public bool HasContent { get => this.ContentType is not null; }

    public GenericStyle()
    {
    }

    public override TValue GetAttribute<TValue>(string name)
    {
        return (TValue)ValuePairs
            .Where(pair => pair.Key.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            .First()
            .Value;
    }

    public override GenericStyle<TStyleWidget> GetSubStyle<TStyleWidget>(string name)
    {
        return (GenericStyle<TStyleWidget>)StylePairs
            .Where(pair => pair.Key.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            .First()
            .Value;
    }

    public override void AddContentStyle(Type contentType, IStyle style)
    {
        if (this.HasContent)
        {
            throw new InvalidDataException($"A style can only contain one content element.");
        }

        this.ContentType = contentType;
        this.ContentStyle = style;
    }

    public override void AddSubWidgetStyle(string propertyOrContentTypeName, IStyle style)
    {
        var propertyInfo = Style.FindProperty(typeof(TWidget), propertyOrContentTypeName);
        if (propertyInfo is null)
        {
            // Maybe it's content.
            if (this.CanHaveContent)
            {
                var contentType = Style.FindWidgetType(propertyOrContentTypeName);
                if (contentType is not null)
                {
                    this.AddContentStyle(contentType, style);
                    return;
                }
                else
                {
                    throw new InvalidDataException($"No property or content type named {propertyOrContentTypeName} could be found in {this.TypeName}.");
                }
            }
            else
            {
                throw new InvalidDataException($"No property named {propertyOrContentTypeName} could be found in {this.TypeName}.");
            }
        }

        this.StylePairs[propertyInfo] = style;
    }

    public override void AddAttribute(string propertyName, object value)
    {
        var propertyInfo = Style.FindProperty(typeof(TWidget), propertyName);
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

    public override Type GetPropertyType(string propertyOrContentTypeName)
    {
        var propertyInfo = Style.FindProperty(typeof(TWidget), propertyOrContentTypeName);
        if (propertyInfo is null)
        {
            if (this.CanHaveContent)
            {
                var contentType = Style.FindWidgetType(propertyOrContentTypeName);
                if (contentType is not null)
                {
                    return contentType;
                }
                else
                {
                    throw new InvalidDataException($"No property or content type named {propertyOrContentTypeName} could be found in {this.TypeName}.");
                }
            }
            else
            {
                throw new InvalidDataException($"No property named {propertyOrContentTypeName} could be found in {this.TypeName}.");
            }
        }

        return propertyInfo.PropertyType;
    }

    public override bool TryGetProperty(string propertyName, out PropertyInfo value)
    {
        value = Style.FindProperty(typeof(TWidget), propertyName);
        return value is not null;
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
            target.SetStyle(pair.Value);
        }

        if (this.HasContent)
        {
            if (widget is ContentControl contentWidget)
            {
                if (contentWidget.Content != null)
                {
                    ApplyToContentRecursively(this.ContentStyle, contentWidget.Content);
                }
                else
                {
                    Widget content;
                    try
                    {
                        content = (Widget)Activator.CreateInstance(this.ContentType);
                    }
                    catch (MissingMethodException)
                    {
                        var styledConstructor = this.ContentType.GetConstructor([typeof(string)]);
                        content = (Widget)styledConstructor.Invoke([Stylesheet.DefaultStyleName]);
                    }
                    contentWidget.Content = content;
                    content.SetStyle(this.ContentStyle);
                }
            }
            else
            {
                throw new InvalidOperationException($"Cannot add content to widget of type {widget.GetType().Name}");
            }
        }
    }

    private static void ApplyToContentRecursively(IStyle style, Widget widget, bool throwOnError = true)
    {
        if (style.CanApplyTo(widget))
        {
            widget.SetStyle(style);
        }
        else if (widget is ContentControl singleContentWidget)
        {
            ApplyToContentRecursively(style, singleContentWidget.Content);
        }
        else if (widget is Container containerWidget)
        {
            foreach(var child in containerWidget.Widgets)
            {
                ApplyToContentRecursively(style, child, false);
            }
        }
        else if (throwOnError)
        {
            throw new Exception($"No appropriate child could be found to style within {widget.GetType().Name}");
        }
    }

    public GenericStyle<TWidget> Clone()
    {
        var clone = new GenericStyle<TWidget>();

        clone.ContentStyle = this.ContentStyle;
        clone.ContentType = this.ContentType;

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

    IStyle IStyle.Clone()
    {
        return this.Clone();
    }

    private bool IsStyleableProperty(PropertyInfo propertyInfo)
    {
        var category = propertyInfo.GetCustomAttribute<CategoryAttribute>()?.Category;

        if (propertyInfo.PropertyType.IsAssignableTo(typeof(Style)))
        {
            return true;
        }

        return category switch
        {
            "Appearance" => true,
            "Behavior" => true,
            "Grid" => true,
            "Layout" => true,
            "Transform" => true,
            "Style" => true,
            "Debug" => false,
            null => false,
            _ => throw new NotSupportedException($"Category {category} is not defined as styleable."),
        };
    }
}
