using System.Collections.Generic;
using System.Linq;

namespace DotTiled.Serialization.Tmx;

public abstract partial class TmxReaderBase
{
  internal TileLayer ReadTileLayer(bool dataUsesChunks)
  {
    var id = _reader.GetRequiredAttributeUInt32("id");
    var name = _reader.GetOptionalAttribute("name").GetValueOr("");
    var @class = _reader.GetOptionalAttribute("class").GetValueOr("");
    var x = _reader.GetOptionalAttributeInt32("x").GetValueOr(0);
    var y = _reader.GetOptionalAttributeInt32("y").GetValueOr(0);
    var width = _reader.GetRequiredAttributeInt32("width");
    var height = _reader.GetRequiredAttributeInt32("height");
    var opacity = _reader.GetOptionalAttributeSingle("opacity").GetValueOr(1.0f);
    var visible = _reader.GetOptionalAttributeBoolean("visible").GetValueOr(true);
    var tintColor = _reader.GetOptionalAttributeParseable<TiledColor>("tintcolor");
    var offsetX = _reader.GetOptionalAttributeSingle("offsetx").GetValueOr(0.0f);
    var offsetY = _reader.GetOptionalAttributeSingle("offsety").GetValueOr(0.0f);
    var parallaxX = _reader.GetOptionalAttributeSingle("parallaxx").GetValueOr(1.0f);
    var parallaxY = _reader.GetOptionalAttributeSingle("parallaxy").GetValueOr(1.0f);

    var propertiesCounter = 0;
    List<IProperty> properties = Helpers.ResolveClassProperties(@class, _customTypeResolver);
    Data data = null;

    _reader.ProcessChildren("layer", (r, elementName) => elementName switch
    {
      "data" => () => Helpers.SetAtMostOnce(ref data, ReadData(dataUsesChunks), "Data"),
      "properties" => () => Helpers.SetAtMostOnceUsingCounter(ref properties, Helpers.MergeProperties(properties, ReadProperties()).ToList(), "Properties", ref propertiesCounter),
      _ => r.Skip
    });

    return new TileLayer
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
      Data = data is null ? Optional.Empty : new Optional<Data>(data),
      Properties = properties ?? []
    };
  }

  internal ImageLayer ReadImageLayer()
  {
    var id = _reader.GetRequiredAttributeUInt32("id");
    var name = _reader.GetOptionalAttribute("name").GetValueOr("");
    var @class = _reader.GetOptionalAttribute("class").GetValueOr("");
    var x = _reader.GetOptionalAttributeInt32("x").GetValueOr(0);
    var y = _reader.GetOptionalAttributeInt32("y").GetValueOr(0);
    var opacity = _reader.GetOptionalAttributeSingle("opacity").GetValueOr(1.0f);
    var visible = _reader.GetOptionalAttributeBoolean("visible").GetValueOr(true);
    var tintColor = _reader.GetOptionalAttributeParseable<TiledColor>("tintcolor");
    var offsetX = _reader.GetOptionalAttributeSingle("offsetx").GetValueOr(0.0f);
    var offsetY = _reader.GetOptionalAttributeSingle("offsety").GetValueOr(0.0f);
    var parallaxX = _reader.GetOptionalAttributeSingle("parallaxx").GetValueOr(1.0f);
    var parallaxY = _reader.GetOptionalAttributeSingle("parallaxy").GetValueOr(1.0f);
    var repeatX = _reader.GetOptionalAttributeBoolean("repeatx").GetValueOr(false);
    var repeatY = _reader.GetOptionalAttributeBoolean("repeaty").GetValueOr(false);

    var propertiesCounter = 0;
    List<IProperty> properties = Helpers.ResolveClassProperties(@class, _customTypeResolver);
    Image image = null;

    _reader.ProcessChildren("imagelayer", (r, elementName) => elementName switch
    {
      "image" => () => Helpers.SetAtMostOnce(ref image, ReadImage(), "Image"),
      "properties" => () => Helpers.SetAtMostOnceUsingCounter(ref properties, Helpers.MergeProperties(properties, ReadProperties()).ToList(), "Properties", ref propertiesCounter),
      _ => r.Skip
    });

    return new ImageLayer
    {
      ID = id,
      Name = name,
      Class = @class,
      X = x,
      Y = y,
      Opacity = opacity,
      Visible = visible,
      TintColor = tintColor,
      OffsetX = offsetX,
      OffsetY = offsetY,
      ParallaxX = parallaxX,
      ParallaxY = parallaxY,
      Properties = properties ?? [],
      Image = image,
      RepeatX = repeatX,
      RepeatY = repeatY
    };
  }

  internal Group ReadGroup()
  {
    var id = _reader.GetRequiredAttributeUInt32("id");
    var name = _reader.GetOptionalAttribute("name").GetValueOr("");
    var @class = _reader.GetOptionalAttribute("class").GetValueOr("");
    var opacity = _reader.GetOptionalAttributeSingle("opacity").GetValueOr(1.0f);
    var visible = _reader.GetOptionalAttributeBoolean("visible").GetValueOr(true);
    var tintColor = _reader.GetOptionalAttributeParseable<TiledColor>("tintcolor");
    var offsetX = _reader.GetOptionalAttributeSingle("offsetx").GetValueOr(0.0f);
    var offsetY = _reader.GetOptionalAttributeSingle("offsety").GetValueOr(0.0f);
    var parallaxX = _reader.GetOptionalAttributeSingle("parallaxx").GetValueOr(1.0f);
    var parallaxY = _reader.GetOptionalAttributeSingle("parallaxy").GetValueOr(1.0f);

    var propertiesCounter = 0;
    List<IProperty> properties = Helpers.ResolveClassProperties(@class, _customTypeResolver);
    List<BaseLayer> layers = [];

    _reader.ProcessChildren("group", (r, elementName) => elementName switch
    {
      "properties" => () => Helpers.SetAtMostOnceUsingCounter(ref properties, Helpers.MergeProperties(properties, ReadProperties()).ToList(), "Properties", ref propertiesCounter),
      "layer" => () => layers.Add(ReadTileLayer(false)),
      "objectgroup" => () => layers.Add(ReadObjectLayer()),
      "imagelayer" => () => layers.Add(ReadImageLayer()),
      "group" => () => layers.Add(ReadGroup()),
      _ => r.Skip
    });

    return new Group
    {
      ID = id,
      Name = name,
      Class = @class,
      Opacity = opacity,
      Visible = visible,
      TintColor = tintColor,
      OffsetX = offsetX,
      OffsetY = offsetY,
      ParallaxX = parallaxX,
      ParallaxY = parallaxY,
      Properties = properties ?? [],
      Layers = layers
    };
  }
}
