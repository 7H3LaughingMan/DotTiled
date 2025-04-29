namespace DotTiled.Serialization.Tmx;

public abstract partial class TmxReaderBase
{
  internal Chunk ReadChunk(Optional<DataEncoding> encoding, Optional<DataCompression> compression)
  {
    var x = _reader.GetRequiredAttributeInt32("x");
    var y = _reader.GetRequiredAttributeInt32("y");
    var width = _reader.GetRequiredAttributeInt32("width");
    var height = _reader.GetRequiredAttributeInt32("height");

    var usesTileChildrenInsteadOfRawData = !encoding.HasValue;
    if (usesTileChildrenInsteadOfRawData)
    {
      var globalTileIDsWithFlippingFlags = ReadTileChildrenInWrapper("chunk", _reader);
      var (globalTileIDs, flippingFlags) = ReadAndClearFlippingFlagsFromGIDs(globalTileIDsWithFlippingFlags);
      return new Chunk { X = x, Y = y, Width = width, Height = height, GlobalTileIDs = globalTileIDs, FlippingFlags = flippingFlags };
    }
    else
    {
      var globalTileIDsWithFlippingFlags = ReadRawData(_reader, encoding.Value, compression);
      var (globalTileIDs, flippingFlags) = ReadAndClearFlippingFlagsFromGIDs(globalTileIDsWithFlippingFlags);
      return new Chunk { X = x, Y = y, Width = width, Height = height, GlobalTileIDs = globalTileIDs, FlippingFlags = flippingFlags };
    }
  }
}
