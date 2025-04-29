using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DotTiled.Serialization.Tmx;

/// <summary>
/// Base class for Tiled XML format readers.
/// </summary>
public abstract partial class TmxReaderBase
{
  internal Map ReadMap()
  {
    // Attributes
    var version = _reader.GetRequiredAttribute("version");
    var tiledVersion = _reader.GetOptionalAttribute("tiledversion");
    var @class = _reader.GetOptionalAttribute("class").GetValueOr("");
    var orientation = _reader.GetRequiredAttributeEnum<MapOrientation>("orientation", Helpers.CreateMapper<MapOrientation>(
      s => throw new InvalidOperationException($"Unknown orientation '{s}'"),
      ("orthogonal", MapOrientation.Orthogonal),
      ("isometric", MapOrientation.Isometric),
      ("staggered", MapOrientation.Staggered),
      ("hexagonal", MapOrientation.Hexagonal)
    ));
    var renderOrder = _reader.GetOptionalAttributeEnum<RenderOrder>("renderorder", Helpers.CreateMapper<RenderOrder>(
      s => throw new InvalidOperationException($"Unknown render order '{s}'"),
      ("right-down", RenderOrder.RightDown),
      ("right-up", RenderOrder.RightUp),
      ("left-down", RenderOrder.LeftDown),
      ("left-up", RenderOrder.LeftUp)
    )).GetValueOr(RenderOrder.RightDown);
    var compressionLevel = _reader.GetOptionalAttributeInt32("compressionlevel").GetValueOr(-1);
    var width = _reader.GetRequiredAttributeInt32("width");
    var height = _reader.GetRequiredAttributeInt32("height");
    var tileWidth = _reader.GetRequiredAttributeInt32("tilewidth");
    var tileHeight = _reader.GetRequiredAttributeInt32("tileheight");
    var hexSideLength = _reader.GetOptionalAttributeInt32("hexsidelength");
    var staggerAxis = _reader.GetOptionalAttributeEnum<StaggerAxis>("staggeraxis", Helpers.CreateMapper<StaggerAxis>(
      s => throw new InvalidOperationException($"Unknown stagger axis '{s}'"),
      ("x", StaggerAxis.X),
      ("y", StaggerAxis.Y)
    ));
    var staggerIndex = _reader.GetOptionalAttributeEnum<StaggerIndex>("staggerindex", Helpers.CreateMapper<StaggerIndex>(
      s => throw new InvalidOperationException($"Unknown stagger index '{s}'"),
      ("odd", StaggerIndex.Odd),
      ("even", StaggerIndex.Even)
    ));
    var parallaxOriginX = _reader.GetOptionalAttributeSingle("parallaxoriginx").GetValueOr(0.0f);
    var parallaxOriginY = _reader.GetOptionalAttributeSingle("parallaxoriginy").GetValueOr(0.0f);
    var backgroundColor = _reader.GetOptionalAttributeParseable<TiledColor>("backgroundcolor").GetValueOr(TiledColor.Parse("#00000000", CultureInfo.InvariantCulture));
    var nextLayerID = _reader.GetRequiredAttributeUInt32("nextlayerid");
    var nextObjectID = _reader.GetRequiredAttributeUInt32("nextobjectid");
    var infinite = _reader.GetOptionalAttributeBoolean("infinite").GetValueOr(false);

    // At most one of
    var propertiesCounter = 0;
    List<IProperty> properties = Helpers.ResolveClassProperties(@class, _customTypeResolver);

    // Any number of
    List<BaseLayer> layers = [];
    List<Tileset> tilesets = [];

    _reader.ProcessChildren("map", (r, elementName) => elementName switch
    {
      "properties" => () => Helpers.SetAtMostOnceUsingCounter(ref properties, Helpers.MergeProperties(properties, ReadProperties()).ToList(), "Properties", ref propertiesCounter),
      "tileset" => () => tilesets.Add(ReadTileset(version, tiledVersion)),
      "layer" => () => layers.Add(ReadTileLayer(infinite)),
      "objectgroup" => () => layers.Add(ReadObjectLayer()),
      "imagelayer" => () => layers.Add(ReadImageLayer()),
      "group" => () => layers.Add(ReadGroup()),
      _ => r.Skip
    });

    return new Map
    {
      Version = version,
      TiledVersion = tiledVersion,
      Class = @class,
      Orientation = orientation,
      RenderOrder = renderOrder,
      CompressionLevel = compressionLevel,
      Width = width,
      Height = height,
      TileWidth = tileWidth,
      TileHeight = tileHeight,
      HexSideLength = hexSideLength,
      StaggerAxis = staggerAxis,
      StaggerIndex = staggerIndex,
      ParallaxOriginX = parallaxOriginX,
      ParallaxOriginY = parallaxOriginY,
      BackgroundColor = backgroundColor,
      NextLayerID = nextLayerID,
      NextObjectID = nextObjectID,
      Infinite = infinite,
      Properties = properties ?? [],
      Tilesets = tilesets,
      Layers = layers
    };
  }
}
