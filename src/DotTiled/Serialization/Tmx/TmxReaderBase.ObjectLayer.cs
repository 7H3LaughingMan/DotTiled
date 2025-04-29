using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;

namespace DotTiled.Serialization.Tmx;

public abstract partial class TmxReaderBase
{
  internal ObjectLayer ReadObjectLayer()
  {
    // Attributes
    var id = _reader.GetRequiredAttributeUInt32("id");
    var name = _reader.GetOptionalAttribute("name").GetValueOr("");
    var @class = _reader.GetOptionalAttribute("class").GetValueOr("");
    var x = _reader.GetOptionalAttributeInt32("x").GetValueOr(0);
    var y = _reader.GetOptionalAttributeInt32("y").GetValueOr(0);
    var width = _reader.GetOptionalAttributeInt32("width").GetValueOr(0);
    var height = _reader.GetOptionalAttributeInt32("height").GetValueOr(0);
    var opacity = _reader.GetOptionalAttributeSingle("opacity").GetValueOr(1.0f);
    var visible = _reader.GetOptionalAttributeBoolean("visible").GetValueOr(true);
    var tintColor = _reader.GetOptionalAttributeParseable<TiledColor>("tintcolor");
    var offsetX = _reader.GetOptionalAttributeSingle("offsetx").GetValueOr(0.0f);
    var offsetY = _reader.GetOptionalAttributeSingle("offsety").GetValueOr(0.0f);
    var parallaxX = _reader.GetOptionalAttributeSingle("parallaxx").GetValueOr(1.0f);
    var parallaxY = _reader.GetOptionalAttributeSingle("parallaxy").GetValueOr(1.0f);
    var color = _reader.GetOptionalAttributeParseable<TiledColor>("color");
    var drawOrder = _reader.GetOptionalAttributeEnum<DrawOrder>("draworder", Helpers.CreateMapper<DrawOrder>(
      s => throw new InvalidOperationException($"Unknown draw order '{s}'"),
      ("topdown", DrawOrder.TopDown),
      ("index", DrawOrder.Index)
    )).GetValueOr(DrawOrder.TopDown);

    // Elements
    var propertiesCounter = 0;
    List<IProperty> properties = Helpers.ResolveClassProperties(@class, _customTypeResolver);
    List<DotTiled.Object> objects = [];

    _reader.ProcessChildren("objectgroup", (r, elementName) => elementName switch
    {
      "properties" => () => Helpers.SetAtMostOnceUsingCounter(ref properties, Helpers.MergeProperties(properties, ReadProperties()).ToList(), "Properties", ref propertiesCounter),
      "object" => () => objects.Add(ReadObject()),
      _ => r.Skip
    });

    return new ObjectLayer
    {
      ID = id,
      Name = name,
      Class = @class,
      X = x,
      Y = y,
      Width = width,
      Height = height,
      Opacity = opacity,
      Visible = visible,
      TintColor = tintColor,
      OffsetX = offsetX,
      OffsetY = offsetY,
      ParallaxX = parallaxX,
      ParallaxY = parallaxY,
      Color = color,
      Properties = properties ?? [],
      DrawOrder = drawOrder,
      Objects = objects
    };
  }

  internal DotTiled.Object ReadObject()
  {
    // Attributes
    var template = _reader.GetOptionalAttribute("template");
    DotTiled.Object obj = null;
    if (template.HasValue)
      obj = _externalTemplateResolver(template.Value).Object.Clone();

    uint idDefault = obj?.ID.GetValueOr(0) ?? 0;
    string nameDefault = obj?.Name ?? "";
    string typeDefault = obj?.Type ?? "";
    float xDefault = obj?.X ?? 0f;
    float yDefault = obj?.Y ?? 0f;
    float widthDefault = obj?.Width ?? 0f;
    float heightDefault = obj?.Height ?? 0f;
    float rotationDefault = obj?.Rotation ?? 0f;
    Optional<uint> gidDefault = obj is TileObject tileObj ? tileObj.GID : Optional.Empty;
    bool visibleDefault = obj?.Visible ?? true;
    List<IProperty> propertiesDefault = obj?.Properties ?? null;

    var id = _reader.GetOptionalAttributeUInt32("id").GetValueOr(idDefault);
    var name = _reader.GetOptionalAttribute("name").GetValueOr(nameDefault);
    var type = _reader.GetOptionalAttribute("type").GetValueOr(typeDefault);
    var x = _reader.GetOptionalAttributeSingle("x").GetValueOr(xDefault);
    var y = _reader.GetOptionalAttributeSingle("y").GetValueOr(yDefault);
    var width = _reader.GetOptionalAttributeSingle("width").GetValueOr(widthDefault);
    var height = _reader.GetOptionalAttributeSingle("height").GetValueOr(heightDefault);
    var rotation = _reader.GetOptionalAttributeSingle("rotation").GetValueOr(rotationDefault);
    var gid = _reader.GetOptionalAttributeUInt32("gid").GetValueOrOptional(gidDefault);
    var visible = _reader.GetOptionalAttributeBoolean("visible").GetValueOr(visibleDefault);

    // Elements
    DotTiled.Object foundObject = null;
    int propertiesCounter = 0;
    List<IProperty> properties = Helpers.ResolveClassProperties(type, _customTypeResolver) ?? propertiesDefault;

    _reader.ProcessChildren("object", (r, elementName) => elementName switch
    {
      "properties" => () => Helpers.SetAtMostOnceUsingCounter(ref properties, Helpers.MergeProperties(properties, ReadProperties()).ToList(), "Properties", ref propertiesCounter),
      "ellipse" => () => Helpers.SetAtMostOnce(ref foundObject, ReadEllipseObject(), "Object marker"),
      "point" => () => Helpers.SetAtMostOnce(ref foundObject, ReadPointObject(), "Object marker"),
      "polygon" => () => Helpers.SetAtMostOnce(ref foundObject, ReadPolygonObject(), "Object marker"),
      "polyline" => () => Helpers.SetAtMostOnce(ref foundObject, ReadPolylineObject(), "Object marker"),
      "text" => () => Helpers.SetAtMostOnce(ref foundObject, ReadTextObject(), "Object marker"),
      _ => throw new InvalidOperationException($"Unknown object marker '{elementName}'")
    });

    if (foundObject is null)
    {
      if (gid.HasValue)
      {
        var (clearedGIDs, flippingFlags) = Helpers.ReadAndClearFlippingFlagsFromGIDs([gid.Value]);
        foundObject = new TileObject { ID = id, GID = clearedGIDs.Single(), FlippingFlags = flippingFlags.Single() };
      }
      else
      {
        foundObject = new RectangleObject { ID = id };
      }
    }

    foundObject.ID = id;
    foundObject.Name = name;
    foundObject.Type = type;
    foundObject.X = x;
    foundObject.Y = y;
    foundObject.Width = width;
    foundObject.Height = height;
    foundObject.Rotation = rotation;
    foundObject.Visible = visible;
    foundObject.Properties = properties ?? [];
    foundObject.Template = template;

    return OverrideObject(obj, foundObject);
  }

  internal static DotTiled.Object OverrideObject(DotTiled.Object obj, DotTiled.Object foundObject)
  {
    if (obj is null)
      return foundObject;

    obj.ID = foundObject.ID;
    obj.Name = foundObject.Name;
    obj.Type = foundObject.Type;
    obj.X = foundObject.X;
    obj.Y = foundObject.Y;
    obj.Width = foundObject.Width;
    obj.Height = foundObject.Height;
    obj.Rotation = foundObject.Rotation;
    obj.Visible = foundObject.Visible;
    obj.Properties = Helpers.MergeProperties(obj.Properties, foundObject.Properties).ToList();
    obj.Template = foundObject.Template;

    if (obj.GetType() != foundObject.GetType())
    {
      return obj;
    }

    return OverrideObject((dynamic)obj, (dynamic)foundObject);
  }

  internal EllipseObject ReadEllipseObject()
  {
    _reader.Skip();
    return new EllipseObject { };
  }

  internal static EllipseObject OverrideObject(EllipseObject obj, EllipseObject _) => obj;

  internal PointObject ReadPointObject()
  {
    _reader.Skip();
    return new PointObject { };
  }

  internal static PointObject OverrideObject(PointObject obj, PointObject _) => obj;

  internal PolygonObject ReadPolygonObject()
  {
    // Attributes
    var points = _reader.GetRequiredAttributeParseable<List<Vector2>>("points", s =>
    {
      // Takes on format "x1,y1 x2,y2 x3,y3 ..."
      return s
        .Split(' ')
        .Select(c => c.Split(','))
        .Select(xy => new Vector2(float.Parse(xy[0], CultureInfo.InvariantCulture), float.Parse(xy[1], CultureInfo.InvariantCulture))).ToList();
    });

    _reader.ReadStartElement("polygon");
    return new PolygonObject { Points = points };
  }

  internal static PolygonObject OverrideObject(PolygonObject obj, PolygonObject foundObject)
  {
    obj.Points = foundObject.Points;
    return obj;
  }

  internal PolylineObject ReadPolylineObject()
  {
    // Attributes
    var points = _reader.GetRequiredAttributeParseable<List<Vector2>>("points", s =>
    {
      // Takes on format "x1,y1 x2,y2 x3,y3 ..."
      return s
        .Split(' ')
        .Select(c => c.Split(','))
        .Select(xy => new Vector2(float.Parse(xy[0], CultureInfo.InvariantCulture), float.Parse(xy[1], CultureInfo.InvariantCulture))).ToList();
    });

    _reader.ReadStartElement("polyline");
    return new PolylineObject { Points = points };
  }

  internal static PolylineObject OverrideObject(PolylineObject obj, PolylineObject foundObject)
  {
    obj.Points = foundObject.Points;
    return obj;
  }

  internal static RectangleObject OverrideObject(RectangleObject obj, RectangleObject foundObject)
  {
    obj.Width = foundObject.Width;
    obj.Height = foundObject.Height;
    return obj;
  }

  internal TextObject ReadTextObject()
  {
    // Attributes
    var fontFamily = _reader.GetOptionalAttribute("fontfamily").GetValueOr("sans-serif");
    var pixelSize = _reader.GetOptionalAttributeInt32("pixelsize").GetValueOr(16);
    var wrap = _reader.GetOptionalAttributeBoolean("wrap").GetValueOr(false);
    var color = _reader.GetOptionalAttributeParseable<TiledColor>("color").GetValueOr(TiledColor.Parse("#000000", CultureInfo.InvariantCulture));
    var bold = _reader.GetOptionalAttributeBoolean("bold").GetValueOr(false);
    var italic = _reader.GetOptionalAttributeBoolean("italic").GetValueOr(false);
    var underline = _reader.GetOptionalAttributeBoolean("underline").GetValueOr(false);
    var strikeout = _reader.GetOptionalAttributeBoolean("strikeout").GetValueOr(false);
    var kerning = _reader.GetOptionalAttributeBoolean("kerning").GetValueOr(true);
    var hAlign = _reader.GetOptionalAttributeEnum<TextHorizontalAlignment>("halign", Helpers.CreateMapper<TextHorizontalAlignment>(
      s => throw new InvalidOperationException($"Unknown horizontal alignment '{s}'"),
      ("left", TextHorizontalAlignment.Left),
      ("center", TextHorizontalAlignment.Center),
      ("right", TextHorizontalAlignment.Right),
      ("justify", TextHorizontalAlignment.Justify)
    )).GetValueOr(TextHorizontalAlignment.Left);
    var vAlign = _reader.GetOptionalAttributeEnum<TextVerticalAlignment>("valign", Helpers.CreateMapper<TextVerticalAlignment>(
      s => throw new InvalidOperationException($"Unknown vertical alignment '{s}'"),
      ("top", TextVerticalAlignment.Top),
      ("center", TextVerticalAlignment.Center),
      ("bottom", TextVerticalAlignment.Bottom)
    )).GetValueOr(TextVerticalAlignment.Top);

    // Elements
    var text = _reader.ReadElementContentAsString("text", "");

    return new TextObject
    {
      FontFamily = fontFamily,
      PixelSize = pixelSize,
      Wrap = wrap,
      Color = color,
      Bold = bold,
      Italic = italic,
      Underline = underline,
      Strikeout = strikeout,
      Kerning = kerning,
      HorizontalAlignment = hAlign,
      VerticalAlignment = vAlign,
      Text = text
    };
  }

  internal static TextObject OverrideObject(TextObject obj, TextObject foundObject)
  {
    obj.FontFamily = foundObject.FontFamily;
    obj.PixelSize = foundObject.PixelSize;
    obj.Wrap = foundObject.Wrap;
    obj.Color = foundObject.Color;
    obj.Bold = foundObject.Bold;
    obj.Italic = foundObject.Italic;
    obj.Underline = foundObject.Underline;
    obj.Strikeout = foundObject.Strikeout;
    obj.Kerning = foundObject.Kerning;
    obj.HorizontalAlignment = foundObject.HorizontalAlignment;
    obj.VerticalAlignment = foundObject.VerticalAlignment;
    obj.Text = foundObject.Text;
    return obj;
  }

  internal static TileObject OverrideObject(TileObject obj, TileObject foundObject)
  {
    obj.GID = foundObject.GID;
    return obj;
  }

  internal Template ReadTemplate()
  {
    // No attributes

    // At most one of
    Tileset tileset = null;

    // Should contain exactly one of
    DotTiled.Object obj = null;

    _reader.ProcessChildren("template", (r, elementName) => elementName switch
    {
      "tileset" => () => Helpers.SetAtMostOnce(ref tileset, ReadTileset(), "Tileset"),
      "object" => () => Helpers.SetAtMostOnce(ref obj, ReadObject(), "Object"),
      _ => r.Skip
    });

    if (obj is null)
      throw new NotSupportedException("Template must contain exactly one object");

    return new Template
    {
      Tileset = tileset,
      Object = obj
    };
  }
}
