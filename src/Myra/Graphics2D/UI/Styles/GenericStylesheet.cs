using FontStashSharp;
using FontStashSharp.RichText;
using Microsoft.Xna.Framework;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.TextureAtlases;
using Myra.MML;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Myra.Graphics2D.UI.Styles;

public class Stylesheet
{
    private static readonly Dictionary<string, string> LegacyClassNames = new Dictionary<string, string>();
    private static readonly Dictionary<string, string> LegacyPropertyNames = new Dictionary<string, string>();
    private static readonly Dictionary<string, string> LegacyWidgetNames = new Dictionary<string, string>();
    private static readonly Dictionary<Type, Type> PropertyTypeSpecializations = new Dictionary<Type, Type>();
    private static readonly Dictionary<Type, string[]> IgnorableProperties = new Dictionary<Type, string[]>();

    public const string DefaultStyleName = "";

    public static Stylesheet Current
    {
        get
        {
            if (field is null)
            {
                field = DefaultAssets.DefaultStylesheet;
            }

            return field;
        }

        set;
    }

    private readonly Dictionary<Type, IDictionary<string, IStyle>> Styles = new();

    public TextureRegionAtlas Atlas { get; private set; }

    public TextureRegion WhiteRegion
    {
        get
        {
            if (field is null)
            {
                field = Atlas["white"];
            }

            return field;
        }
    }

    public Dictionary<string, SpriteFontBase> Fonts { get; private set; }

    public DesktopStyle DesktopStyle { get; set; }

    public Stylesheet()
    {
        var defaultWidgetStyle = new GenericStyle<Widget>();
        this.AddStyle(defaultWidgetStyle);
    }

    static Stylesheet()
    {
#pragma warning disable CS0618 // Type or member is obsolete
        LegacyClassNames["TextBlockStyle"] = "LabelStyle";
        LegacyClassNames["TextFieldStyle"] = "TextBoxStyle";
        LegacyClassNames["ScrollPaneStyle"] = "ScrollViewerStyle";

        LegacyPropertyNames["TextBlockStyle"] = "LabelStyle";
        LegacyPropertyNames["TextFieldStyle"] = "TextBoxStyle";
        LegacyPropertyNames["ScrollPaneStyle"] = "ScrollViewerStyle";
        LegacyPropertyNames["TextBlockStyles"] = "LabelStyles";
        LegacyPropertyNames["TextFieldStyles"] = "TextBoxStyles";
        LegacyPropertyNames["ScrollPaneStyles"] = "ScrollViewerStyles";

        LegacyWidgetNames["CheckBox"] = "ImageTextButton";

        IgnorableProperties[typeof(ComboView)] = ["LabelStyle"];
        IgnorableProperties[typeof(ComboBox)] = ["LabelStyle"];
#pragma warning restore CS0618 // Type or member is obsolete
    }

    public void CombineWith(Stylesheet other)
    {

    }

    public IStyle GetStyleFor(object target, string name = Stylesheet.DefaultStyleName)
    {
        var targetType = target.GetType();
        return GetStyle(targetType, name);
    }

    public IStyle<TWidget> GetStyle<TWidget>(string name = Stylesheet.DefaultStyleName)
        where TWidget : Widget
    {
        var targetType = typeof(TWidget);
        return (IStyle<TWidget>)GetStyle(targetType, name);
    }

    public IStyle GetStyle(Type widgetType, string name = Stylesheet.DefaultStyleName)
    {
        IDictionary<string, IStyle> styles = null;
        IStyle style = null;
        if (string.IsNullOrEmpty(name))
        {
            name = Stylesheet.DefaultStyleName;
        }

        while (widgetType != typeof(object) && !this.Styles.TryGetValue(widgetType, out styles))
        {
            widgetType = widgetType.BaseType;
        }

        if (styles is not null)
        {
            styles.TryGetValue(name, out style);
        }

        return style;
    }

    public void AddStyle(IStyle style, string name = Stylesheet.DefaultStyleName)
    {
        var styleType = style.GetType();
        if (styleType.IsGenericType)
        {
            IDictionary<string, IStyle> dict;
            var targetWidget = styleType.GetGenericArguments()[0];
            if (!this.Styles.TryGetValue(targetWidget, out dict))
            {
                dict = new Dictionary<string, IStyle>();
                this.Styles.Add(targetWidget, dict);
            }

            dict.Add(name, style);
        }
    }

    public string[] GetStylesByWidgetName(string name)
    {
        var dict = this.Styles
            .Where(pair => pair.Key.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault()
            .Value;

        if (dict is null)
        {
            return null;
        }

        var result = new List<string>();
        foreach (var k in dict.Keys)
        {
            result.Add((string)k);
        }

        return result.ToArray();
    }

    public static Stylesheet LoadFromSource(string stylesheetXml,
            TextureRegionAtlas textureRegionAtlas,
            Dictionary<string, SpriteFontBase> fonts)
    {
        var xDoc = XDocument.Parse(stylesheetXml);

        var colors = new Dictionary<string, Color>();
        var colorsNode = xDoc.Root.Element("Colors");
        if (colorsNode != null)
        {
            foreach (var el in colorsNode.Elements())
            {
                var color = ColorStorage.FromName(el.Attribute("Value").Value);
                if (color != null)
                {
                    colors[el.Attribute(BaseContext.IdName).Value] = color.Value;
                }
            }
        }

        Func<Type, string, object> resourceGetter = (t, name) =>
        {
            if (typeof(IBrush).IsAssignableFrom(t))
            {
                TextureRegion region;

                if (!textureRegionAtlas.Regions.TryGetValue(name, out region))
                {
                    var color = ColorStorage.FromName(name);
                    if (color != null)
                    {
                        return new SolidBrush(color.Value);
                    }
                }
                else
                {
                    return region;
                }

                throw new Exception(string.Format("Could not find parse IBrush '{0}'", name));
            }
            else if (t == typeof(SpriteFontBase))
            {
                return fonts[name];
            }

            throw new Exception(string.Format("Type {0} isn't supported", t.Name));
        };

        var result = new Stylesheet
        {
            Atlas = textureRegionAtlas,
            Fonts = fonts
        };

        var loadContext = new LoadContext
        {
            Assemblies = new Dictionary<Assembly, string[]>()
                {
                    { typeof( WidgetStyle ).Assembly, new string[] { typeof( WidgetStyle ).Namespace } }
                },
            ResourceGetter = resourceGetter,
            NodesToIgnore = new HashSet<string>(new[] { "Designer", "Colors", "Fonts" }),
            LegacyClassNames = LegacyClassNames,
            LegacyPropertyNames = LegacyPropertyNames,
            Colors = colors
        };

        foreach(var child in xDoc.Root.Elements())
        {
            if (child.Name.LocalName.EndsWith("Styles"))
            {
                var dict = Stylesheet.LoadStylesFromXml(child, loadContext, out var targetType);
                result.Styles[targetType] = dict;
            }
        }

        return result;
    }
    private static IDictionary<string, IStyle> LoadStylesFromXml(XElement stylesElement,
        LoadContext context,
        out Type targetType)
    {
        var name = stylesElement.Name.LocalName;

        if (!name.EndsWith("Styles", StringComparison.OrdinalIgnoreCase))
        {
            throw new FormatException($"Cannot load styles from XElement {name} because it is not a styles element.");
        }

        var targetTypeName = name.Substring(0, name.Length - "Styles".Length);
        if (LegacyWidgetNames.TryGetValue(targetTypeName, out var modernName))
        {
            targetTypeName = modernName;
        }

        targetType = GenericStyle.FindWidgetType(targetTypeName);

        if (targetType is null)
        {
            throw new Exception($"Could not parse styles for {name} because no matching type was found.");
        }

        var styleType = typeof(GenericStyle<>).MakeGenericType(targetType);

        var ret = new Dictionary<string, IStyle>();

        foreach (var styleElement in stylesElement.Elements())
        {
            var id = styleElement.Attribute(BaseContext.IdName)?.Value;
            var styleTarget = Activator.CreateInstance(styleType) as GenericStyle;
            styleTarget.Name = id ?? Stylesheet.DefaultStyleName;
            var parentId = $"{stylesElement.Name}/id";
            Stylesheet.PopulateStyleFromXml(styleTarget, styleElement, context, parentId);

            ret[styleTarget.Name] = (IStyle)styleTarget;
        }

        return ret;
    }

    private static void PopulateStyleFromXml(GenericStyle target,
        XElement styleElement,
        LoadContext context,
        string currentId)
    {
        var targetType = target.TargetType;

        foreach (var xAttribute in styleElement.Attributes())
        {
            var name = xAttribute.Name.LocalName;
            var propertyType = target.GetPropertyType(name);
            if (name == BaseContext.IdName)
            {
                continue;
            }

            var value = context.ReadSimplePropertyFromAttribute(xAttribute, null, propertyType, name);
            target.AddAttribute(name, value);
        }

        foreach (var childElement in styleElement.Elements())
        {
            var name = childElement.Name.LocalName;
            string error = $"{name} is not a styleable property of {targetType.Name}.";
            bool success = false;

            if (target.TryGetProperty(name, out var property))
            {
                if (property.PropertyType.IsAssignableTo(typeof(IStyle)))
                {
                    var styleType = property.PropertyType;
                    if(styleType.IsInterface)
                    {
                        styleType = typeof(GenericStyle<>).MakeGenericType(styleType.GenericTypeArguments);
                    }

                    var subStyleTarget = (GenericStyle)Activator.CreateInstance(styleType);
                    var childId = $"{currentId}/{name}";
                    subStyleTarget.Name = childId;
                    Stylesheet.PopulateStyleFromXml(subStyleTarget, childElement, context, childId);
                    target.AddAttribute(name, subStyleTarget);
                    success = true;
                }
                else
                {
                    error = $"{name} is a full element in the stylesheet under {targetType}Style, but is not a style";
                }
            }
            else if (name.EndsWith("Style"))
            {
                var styleablePropertyName = name.Substring(0, name.Length - "Style".Length);
                try
                {
                    var propertyType = target.GetPropertyType(styleablePropertyName);
                    if (PropertyTypeSpecializations.TryGetValue(propertyType, out var specializedType))
                    {
                        propertyType = specializedType;
                    }

                    if (propertyType.IsAssignableTo(typeof(Widget)))
                    {
                        var propertyStyleType = typeof(GenericStyle<>).MakeGenericType([propertyType]);
                        var subStyleTarget = (GenericStyle)Activator.CreateInstance(propertyStyleType);
                        var childId = $"{currentId}/{name}";
                        subStyleTarget.Name = childId;
                        Stylesheet.PopulateStyleFromXml(subStyleTarget, childElement, context, childId);
                        target.AddSubWidgetStyle(styleablePropertyName, (IStyle)subStyleTarget);
                        success = true;
                    }
                    else if (target.CanHaveContent)
                    {
                        var contentWidgetType = GenericStyle.FindWidgetType(styleablePropertyName);
                        error = $"{name} is not a styleable property of {targetType.Name} or valid content type";

                        if (contentWidgetType is not null)
                        {
                            var propertyStyleType = typeof(GenericStyle<>).MakeGenericType([contentWidgetType]);
                            var subStyleTarget = Activator.CreateInstance(propertyStyleType) as GenericStyle;
                            var childId = $"{currentId}/{name}";
                            subStyleTarget.Name = childId;
                            Stylesheet.PopulateStyleFromXml(subStyleTarget, childElement, context, childId);
                            target.AddContentStyle(contentWidgetType, (IStyle)subStyleTarget);
                            success = true;
                        }
                    }
                }
                catch (InvalidDataException e)
                {
                    error = e.Message;
                }
            }
            else
            {
                error = $"Unable to process property {name} of {targetType}Style because it is a full element but not a style.";
            }

            if (!success && !(IgnorableProperties.TryGetValue(target.TargetType, out var ignorableProperties) && ignorableProperties.Contains(name)))
            {
                throw new Exception(error);
            }
        }
    }
}
