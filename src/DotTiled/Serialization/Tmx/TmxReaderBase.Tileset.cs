using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DotTiled.Serialization.Tmx;

public abstract partial class TmxReaderBase
{
  internal Tileset ReadTileset(
    Optional<string> parentVersion = default,
    Optional<string> parentTiledVersion = default)
  {
    var firstGID = _reader.GetOptionalAttributeUInt32("firstgid");
    var source = _reader.GetOptionalAttribute("source");

    // Check if external tileset
    if (source.HasValue && firstGID.HasValue)
    {
      // Is external tileset
      var externalTileset = _externalTilesetResolver(source.Value);
      externalTileset.FirstGID = firstGID;
      externalTileset.Source = source;

      _reader.ProcessChildren("tileset", (r, elementName) => r.Skip);
      return externalTileset;
    }

    // Attributes
    var version = _reader.GetOptionalAttribute("version").GetValueOrOptional(parentVersion);
    var tiledVersion = _reader.GetOptionalAttribute("tiledversion").GetValueOrOptional(parentTiledVersion);
    var name = _reader.GetRequiredAttribute("name");
    var @class = _reader.GetOptionalAttribute("class").GetValueOr("");
    var tileWidth = _reader.GetRequiredAttributeInt32("tilewidth");
    var tileHeight = _reader.GetRequiredAttributeInt32("tileheight");
    var spacing = _reader.GetOptionalAttributeInt32("spacing").GetValueOr(0);
    var margin = _reader.GetOptionalAttributeInt32("margin").GetValueOr(0);
    var tileCount = _reader.GetRequiredAttributeInt32("tilecount");
    var columns = _reader.GetRequiredAttributeInt32("columns");
    var objectAlignment = _reader.GetOptionalAttributeEnum<ObjectAlignment>("objectalignment", Helpers.CreateMapper<ObjectAlignment>(
      s => throw new InvalidOperationException($"Unknown object alignment '{s}'"),
      ("unspecified", ObjectAlignment.Unspecified),
      ("topleft", ObjectAlignment.TopLeft),
      ("top", ObjectAlignment.Top),
      ("topright", ObjectAlignment.TopRight),
      ("left", ObjectAlignment.Left),
      ("center", ObjectAlignment.Center),
      ("right", ObjectAlignment.Right),
      ("bottomleft", ObjectAlignment.BottomLeft),
      ("bottom", ObjectAlignment.Bottom),
      ("bottomright", ObjectAlignment.BottomRight)
    )).GetValueOr(ObjectAlignment.Unspecified);
    var renderSize = _reader.GetOptionalAttributeEnum<TileRenderSize>("tilerendersize", Helpers.CreateMapper<TileRenderSize>(
      s => throw new InvalidOperationException($"Unknown render size '{s}'"),
      ("tile", TileRenderSize.Tile),
      ("grid", TileRenderSize.Grid)
    )).GetValueOr(TileRenderSize.Tile);
    var fillMode = _reader.GetOptionalAttributeEnum<FillMode>("fillmode", Helpers.CreateMapper<FillMode>(
      s => throw new InvalidOperationException($"Unknown fill mode '{s}'"),
      ("stretch", FillMode.Stretch),
      ("preserve-aspect-fit", FillMode.PreserveAspectFit)
    )).GetValueOr(FillMode.Stretch);

    // Elements
    Image image = null;
    TileOffset tileOffset = null;
    Grid grid = null;
    var propertiesCounter = 0;
    List<IProperty> properties = Helpers.ResolveClassProperties(@class, _customTypeResolver);
    List<Wangset> wangsets = null;
    Transformations transformations = null;
    List<Tile> tiles = [];

    _reader.ProcessChildren("tileset", (r, elementName) => elementName switch
    {
      "image" => () => Helpers.SetAtMostOnce(ref image, ReadImage(), "Image"),
      "tileoffset" => () => Helpers.SetAtMostOnce(ref tileOffset, ReadTileOffset(), "TileOffset"),
      "grid" => () => Helpers.SetAtMostOnce(ref grid, ReadGrid(), "Grid"),
      "properties" => () => Helpers.SetAtMostOnceUsingCounter(ref properties, Helpers.MergeProperties(properties, ReadProperties()).ToList(), "Properties", ref propertiesCounter),
      "wangsets" => () => Helpers.SetAtMostOnce(ref wangsets, ReadWangsets(), "Wangsets"),
      "transformations" => () => Helpers.SetAtMostOnce(ref transformations, ReadTransformations(), "Transformations"),
      "tile" => () => tiles.Add(ReadTile()),
      _ => r.Skip
    });

    return new Tileset
    {
      Version = version,
      TiledVersion = tiledVersion,
      FirstGID = firstGID,
      Source = source,
      Name = name,
      Class = @class,
      TileWidth = tileWidth,
      TileHeight = tileHeight,
      Spacing = spacing,
      Margin = margin,
      TileCount = tileCount,
      Columns = columns,
      ObjectAlignment = objectAlignment,
      RenderSize = renderSize,
      FillMode = fillMode,
      Image = image,
      TileOffset = tileOffset,
      Grid = grid,
      Properties = properties ?? [],
      Wangsets = wangsets ?? [],
      Transformations = transformations,
      Tiles = tiles ?? []
    };
  }

  internal Image ReadImage()
  {
    // Attributes
    var format = _reader.GetOptionalAttributeEnum<ImageFormat>("format", Helpers.CreateMapper<ImageFormat>(
      s => throw new InvalidOperationException($"Unknown image format '{s}'"),
      ("png", ImageFormat.Png),
      ("jpg", ImageFormat.Jpg),
      ("bmp", ImageFormat.Bmp),
      ("gif", ImageFormat.Gif)
    ));
    var source = _reader.GetOptionalAttribute("source");
    var transparentColor = _reader.GetOptionalAttributeParseable<TiledColor>("trans");
    var width = _reader.GetOptionalAttributeInt32("width");
    var height = _reader.GetOptionalAttributeInt32("height");

    _reader.ProcessChildren("image", (r, elementName) => elementName switch
    {
      "data" => throw new NotSupportedException("Embedded image data is not supported."),
      _ => r.Skip
    });

    if (!format.HasValue && source.HasValue)
      format = Helpers.ParseImageFormatFromSource(source.Value);

    return new Image
    {
      Format = format,
      Source = source,
      TransparentColor = transparentColor,
      Width = width,
      Height = height,
    };
  }

  internal TileOffset ReadTileOffset()
  {
    // Attributes
    var x = _reader.GetOptionalAttributeSingle("x").GetValueOr(0.0f);
    var y = _reader.GetOptionalAttributeSingle("y").GetValueOr(0.0f);

    _reader.ReadStartElement("tileoffset");
    return new TileOffset { X = x, Y = y };
  }

  internal Grid ReadGrid()
  {
    // Attributes
    var orientation = _reader.GetOptionalAttributeEnum<GridOrientation>("orientation", Helpers.CreateMapper<GridOrientation>(
      s => throw new InvalidOperationException($"Unknown orientation '{s}'"),
      ("orthogonal", GridOrientation.Orthogonal),
      ("isometric", GridOrientation.Isometric)
    )).GetValueOr(GridOrientation.Orthogonal);
    var width = _reader.GetRequiredAttributeInt32("width");
    var height = _reader.GetRequiredAttributeInt32("height");

    _reader.ReadStartElement("grid");
    return new Grid { Orientation = orientation, Width = width, Height = height };
  }

  internal Transformations ReadTransformations()
  {
    // Attributes
    var hFlip = _reader.GetOptionalAttributeBoolean("hflip").GetValueOr(false);
    var vFlip = _reader.GetOptionalAttributeBoolean("vflip").GetValueOr(false);
    var rotate = _reader.GetOptionalAttributeBoolean("rotate").GetValueOr(false);
    var preferUntransformed = _reader.GetOptionalAttributeBoolean("preferuntransformed").GetValueOr(false);

    _reader.ReadStartElement("transformations");
    return new Transformations { HFlip = hFlip, VFlip = vFlip, Rotate = rotate, PreferUntransformed = preferUntransformed };
  }

  internal Tile ReadTile()
  {
    // Attributes
    var id = _reader.GetRequiredAttributeUInt32("id");
    var type = _reader.GetOptionalAttribute("type").GetValueOr("");
    var probability = _reader.GetOptionalAttributeSingle("probability").GetValueOr(0.0f);
    var x = _reader.GetOptionalAttributeInt32("x").GetValueOr(0);
    var y = _reader.GetOptionalAttributeInt32("y").GetValueOr(0);
    var width = _reader.GetOptionalAttributeInt32("width");
    var height = _reader.GetOptionalAttributeInt32("height");

    // Elements
    var propertiesCounter = 0;
    List<IProperty> properties = Helpers.ResolveClassProperties(type, _customTypeResolver);
    Image image = null;
    ObjectLayer objectLayer = null;
    List<Frame> animation = null;

    _reader.ProcessChildren("tile", (r, elementName) => elementName switch
    {
      "properties" => () => Helpers.SetAtMostOnceUsingCounter(ref properties, Helpers.MergeProperties(properties, ReadProperties()).ToList(), "Properties", ref propertiesCounter),
      "image" => () => Helpers.SetAtMostOnce(ref image, ReadImage(), "Image"),
      "objectgroup" => () => Helpers.SetAtMostOnce(ref objectLayer, ReadObjectLayer(), "ObjectLayer"),
      "animation" => () => Helpers.SetAtMostOnce(ref animation, r.ReadList<Frame>("animation", "frame", (ar) =>
      {
        return new Frame
        {
          TileID = ar.GetRequiredAttributeUInt32("tileid"),
          Duration = ar.GetRequiredAttributeInt32("duration")
        };
      }), "Animation"),
      _ => r.Skip
    });

    return new Tile
    {
      ID = id,
      Type = type,
      Probability = probability,
      X = x,
      Y = y,
      Width = width.HasValue ? width.Value : image?.Width.GetValueOr(0) ?? 0,
      Height = height.HasValue ? height.Value : image?.Height.GetValueOr(0) ?? 0,
      Properties = properties ?? [],
      Image = image is null ? Optional.Empty : image,
      ObjectLayer = objectLayer is null ? Optional.Empty : objectLayer,
      Animation = animation ?? []
    };
  }

  internal List<Wangset> ReadWangsets() =>
    _reader.ReadList<Wangset>("wangsets", "wangset", r => ReadWangset());

  internal Wangset ReadWangset()
  {
    // Attributes
    var name = _reader.GetRequiredAttribute("name");
    var @class = _reader.GetOptionalAttribute("class").GetValueOr("");
    var tile = _reader.GetRequiredAttributeInt32("tile");

    // Elements
    var propertiesCounter = 0;
    List<IProperty> properties = Helpers.ResolveClassProperties(@class, _customTypeResolver);
    List<WangColor> wangColors = [];
    List<WangTile> wangTiles = [];

    _reader.ProcessChildren("wangset", (r, elementName) => elementName switch
    {
      "properties" => () => Helpers.SetAtMostOnceUsingCounter(ref properties, Helpers.MergeProperties(properties, ReadProperties()).ToList(), "Properties", ref propertiesCounter),
      "wangcolor" => () => wangColors.Add(ReadWangColor()),
      "wangtile" => () => wangTiles.Add(ReadWangTile()),
      _ => r.Skip
    });

    if (wangColors.Count > 254)
      throw new ArgumentException("Wangset can have at most 254 Wang colors.");

    return new Wangset
    {
      Name = name,
      Class = @class,
      Tile = tile,
      Properties = properties ?? [],
      WangColors = wangColors,
      WangTiles = wangTiles
    };
  }

  internal WangColor ReadWangColor()
  {
    // Attributes
    var name = _reader.GetRequiredAttribute("name");
    var @class = _reader.GetOptionalAttribute("class").GetValueOr("");
    var color = _reader.GetRequiredAttributeParseable<TiledColor>("color");
    var tile = _reader.GetRequiredAttributeInt32("tile");
    var probability = _reader.GetOptionalAttributeSingle("probability").GetValueOr(0f);

    // Elements
    var propertiesCounter = 0;
    List<IProperty> properties = Helpers.ResolveClassProperties(@class, _customTypeResolver);

    _reader.ProcessChildren("wangcolor", (r, elementName) => elementName switch
    {
      "properties" => () => Helpers.SetAtMostOnceUsingCounter(ref properties, Helpers.MergeProperties(properties, ReadProperties()).ToList(), "Properties", ref propertiesCounter),
      _ => r.Skip
    });

    return new WangColor
    {
      Name = name,
      Class = @class,
      Color = color,
      Tile = tile,
      Probability = probability,
      Properties = properties ?? []
    };
  }

  internal WangTile ReadWangTile()
  {
    // Attributes
    var tileID = _reader.GetRequiredAttributeUInt32("tileid");
    var wangID = _reader.GetRequiredAttributeParseable<byte[]>("wangid", s =>
    {
      // Comma-separated list of indices (0-254)
      var indices = s.Split(',').Select(i => byte.Parse(i, CultureInfo.InvariantCulture)).ToArray();
      if (indices.Length > 8)
        throw new ArgumentException("Wang ID can have at most 8 indices.");
      return indices;
    });

    _reader.ReadStartElement("wangtile");

    return new WangTile
    {
      TileID = tileID,
      WangID = wangID
    };
  }
}
