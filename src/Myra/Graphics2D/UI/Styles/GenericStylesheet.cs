using FontStashSharp;
using FontStashSharp.RichText;
using Microsoft.Xna.Framework;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.TextureAtlases;
using Myra.MML;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Myra.Graphics2D.UI.Styles;

public class GenericStylesheet
{
    private static readonly Dictionary<string, string> LegacyClassNames = new Dictionary<string, string>();
    private static readonly Dictionary<string, string> LegacyPropertyNames = new Dictionary<string, string>();
    private static readonly Dictionary<string, string> LegacyWidgetNames = new Dictionary<string, string>();

    private readonly Dictionary<Type, IDictionary<string, IStyle>> Styles = new();

    public static GenericStylesheet Current
    {
        get
        {
            if (field is null)
            {
                field = DefaultAssets.DefaultGenericStylesheet;
            }

            return field;
        }

        set;
    }
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

    public GenericStylesheet()
    {

    }

    static GenericStylesheet()
    {
        LegacyClassNames["TextBlockStyle"] = "LabelStyle";
        LegacyClassNames["TextFieldStyle"] = "TextBoxStyle";
        LegacyClassNames["ScrollPaneStyle"] = "ScrollViewerStyle";

        LegacyPropertyNames["TextBlockStyle"] = "LabelStyle";
        LegacyPropertyNames["TextFieldStyle"] = "TextBoxStyle";
        LegacyPropertyNames["ScrollPaneStyle"] = "ScrollViewerStyle";
        LegacyPropertyNames["TextBlockStyles"] = "LabelStyles";
        LegacyPropertyNames["TextFieldStyles"] = "TextBoxStyles";
        LegacyPropertyNames["ScrollPaneStyles"] = "ScrollViewerStyles";
    }

    public void CombineWith(GenericStylesheet other)
    {

    }

    public static GenericStylesheet LoadFromSource(string stylesheetXml,
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

        var result = new GenericStylesheet
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
                var dict = GenericStylesheet.LoadStylesFromXml(child, loadContext, out var targetType);
                result.Styles[targetType] = dict;
            }
        }

        return result;
    }
    internal static IDictionary<string, IStyle> LoadStylesFromXml(XElement stylesElement,
        LoadContext context,
        out Type targetType)
    {
        var name = stylesElement.Name.LocalName;

        if (!name.EndsWith("Styles", StringComparison.OrdinalIgnoreCase))
        {
            throw new FormatException($"Cannot load styles from XElement {name} because it is not a styles element.");
        }

        var targetTypeName = name.Substring(0, name.Length - "Styles".Length);
        targetType = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes()
                .Where(t => t.Name.Equals(targetTypeName, StringComparison.OrdinalIgnoreCase))
                .Where(t => t.IsAssignableTo(typeof(Widget))))
            .FirstOrDefault();

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
            GenericStylesheet.PopulateStyleFromXml(styleTarget,
                styleElement,
                context);

            ret[id ?? Stylesheet.DefaultStyleName] = (IStyle)styleTarget;
        }

        return ret;
    }

    private static void PopulateStyleFromXml(GenericStyle target,
        XElement styleElement,
        LoadContext context)
    {
        var targetType = target.GetType();

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

            if (name.EndsWith("Style"))
            {
                var styleablePropertyName = name.Substring(0, name.Length - "Style".Length);
                var propertyType = target.GetPropertyType(styleablePropertyName);
                var subStyleTarget = Activator.CreateInstance(propertyType) as GenericStyle;
                GenericStylesheet.PopulateStyleFromXml(subStyleTarget, childElement, context);
                target.AddSubWidgetStyle(styleablePropertyName, (IStyle)subStyleTarget);
            }
        }
    }
}
