using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;

namespace DotTiled.Serialization.Tmx;

internal static class ExtensionsXmlReader
{
  internal static string GetRequiredAttribute(this XmlReader reader, string attribute) => reader.GetAttribute(attribute) ?? throw new XmlException($"{attribute} attribute is required");

  internal static bool GetRequiredAttributeBoolean(this XmlReader reader, string attribute) => XmlConvert.ToBoolean(reader.GetRequiredAttribute(attribute));

  internal static byte GetRequiredAttributeByte(this XmlReader reader, string attribute) => XmlConvert.ToByte(reader.GetRequiredAttribute(attribute));

  internal static char GetRequiredAttributeChar(this XmlReader reader, string attribute) => XmlConvert.ToChar(reader.GetRequiredAttribute(attribute));

  internal static decimal GetRequiredAttributeDecimal(this XmlReader reader, string attribute) => XmlConvert.ToDecimal(reader.GetRequiredAttribute(attribute));

  internal static double GetRequiredAttributeDouble(this XmlReader reader, string attribute) => XmlConvert.ToDouble(reader.GetRequiredAttribute(attribute));

  internal static short GetRequiredAttributeInt16(this XmlReader reader, string attribute) => XmlConvert.ToInt16(reader.GetRequiredAttribute(attribute));

  internal static int GetRequiredAttributeInt32(this XmlReader reader, string attribute) => XmlConvert.ToInt32(reader.GetRequiredAttribute(attribute));

  internal static long GetRequiredAttributeInt64(this XmlReader reader, string attribute) => XmlConvert.ToInt64(reader.GetRequiredAttribute(attribute));

  internal static sbyte GetRequiredAttributeSByte(this XmlReader reader, string attribute) => XmlConvert.ToSByte(reader.GetRequiredAttribute(attribute));

  internal static float GetRequiredAttributeSingle(this XmlReader reader, string attribute) => XmlConvert.ToSingle(reader.GetRequiredAttribute(attribute));

  internal static ushort GetRequiredAttributeUInt16(this XmlReader reader, string attribute) => XmlConvert.ToUInt16(reader.GetRequiredAttribute(attribute));

  internal static uint GetRequiredAttributeUInt32(this XmlReader reader, string attribute) => XmlConvert.ToUInt32(reader.GetRequiredAttribute(attribute));

  internal static ulong GetRequiredAttributeUInt64(this XmlReader reader, string attribute) => XmlConvert.ToUInt64(reader.GetRequiredAttribute(attribute));

  internal static T GetRequiredAttributeParseable<T>(this XmlReader reader, string attribute) where T : IParsable<T> => T.Parse(reader.GetRequiredAttribute(attribute), CultureInfo.InvariantCulture);

  internal static T GetRequiredAttributeParseable<T>(this XmlReader reader, string attribute, Func<string, T> parser) => parser(reader.GetRequiredAttribute(attribute));

  internal static T GetRequiredAttributeEnum<T>(this XmlReader reader, string attribute, Func<string, T> enumParser) where T : Enum => enumParser(reader.GetRequiredAttribute(attribute));

  internal static bool TryGetAttribute(this XmlReader reader, string name, out string value) => (value = reader.GetAttribute(name)) is not null;

  internal static Optional<string> GetOptionalAttribute(this XmlReader reader, string attribute) => reader.TryGetAttribute(attribute, out var value) ? new Optional<string>(value) : new Optional<string>();

  internal static Optional<bool> GetOptionalAttributeBoolean(this XmlReader reader, string attribute) => reader.TryGetAttribute(attribute, out var value) ? new Optional<bool>(XmlConvert.ToBoolean(value)) : new Optional<bool>();

  internal static Optional<byte> GetOptionalAttributeByte(this XmlReader reader, string attribute) => reader.TryGetAttribute(attribute, out var value) ? new Optional<byte>(XmlConvert.ToByte(value)) : new Optional<byte>();

  internal static Optional<char> GetOptionalAttributeChar(this XmlReader reader, string attribute) => reader.TryGetAttribute(attribute, out var value) ? new Optional<char>(XmlConvert.ToChar(value)) : new Optional<char>();

  internal static Optional<decimal> GetOptionalAttributeDecimal(this XmlReader reader, string attribute) => reader.TryGetAttribute(attribute, out var value) ? new Optional<decimal>(XmlConvert.ToDecimal(value)) : new Optional<decimal>();

  internal static Optional<double> GetOptionalAttributeDouble(this XmlReader reader, string attribute) => reader.TryGetAttribute(attribute, out var value) ? new Optional<double>(XmlConvert.ToDouble(value)) : new Optional<double>();

  internal static Optional<short> GetOptionalAttributeInt16(this XmlReader reader, string attribute) => reader.TryGetAttribute(attribute, out var value) ? new Optional<short>(XmlConvert.ToInt16(value)) : new Optional<short>();

  internal static Optional<int> GetOptionalAttributeInt32(this XmlReader reader, string attribute) => reader.TryGetAttribute(attribute, out var value) ? new Optional<int>(XmlConvert.ToInt32(value)) : new Optional<int>();

  internal static Optional<long> GetOptionalAttributeInt64(this XmlReader reader, string attribute) => reader.TryGetAttribute(attribute, out var value) ? new Optional<long>(XmlConvert.ToInt64(value)) : new Optional<long>();

  internal static Optional<sbyte> GetOptionalAttributeSByte(this XmlReader reader, string attribute) => reader.TryGetAttribute(attribute, out var value) ? new Optional<sbyte>(XmlConvert.ToSByte(value)) : new Optional<sbyte>();

  internal static Optional<float> GetOptionalAttributeSingle(this XmlReader reader, string attribute) => reader.TryGetAttribute(attribute, out var value) ? new Optional<float>(XmlConvert.ToSingle(value)) : new Optional<float>();

  internal static Optional<ushort> GetOptionalAttributeUInt16(this XmlReader reader, string attribute) => reader.TryGetAttribute(attribute, out var value) ? new Optional<ushort>(XmlConvert.ToUInt16(value)) : new Optional<ushort>();

  internal static Optional<uint> GetOptionalAttributeUInt32(this XmlReader reader, string attribute) => reader.TryGetAttribute(attribute, out var value) ? new Optional<uint>(XmlConvert.ToUInt32(value)) : new Optional<uint>();

  internal static Optional<ulong> GetOptionalAttributeUInt64(this XmlReader reader, string attribute) => reader.TryGetAttribute(attribute, out var value) ? new Optional<ulong>(XmlConvert.ToUInt64(value)) : new Optional<ulong>();

  internal static Optional<T> GetOptionalAttributeParseable<T>(this XmlReader reader, string attribute) where T : IParsable<T> => reader.TryGetAttribute(attribute, out var value) ? new Optional<T>(T.Parse(value, CultureInfo.InvariantCulture)) : new Optional<T>();

  internal static Optional<T> GetOptionalAttributeParseable<T>(this XmlReader reader, string attribute, Func<string, T> parser) => reader.TryGetAttribute(attribute, out var value) ? new Optional<T>(parser(value)) : new Optional<T>();

  internal static Optional<T> GetOptionalAttributeEnum<T>(this XmlReader reader, string attribute, Func<string, T> enumParser) where T : struct, Enum => reader.TryGetAttribute(attribute, out var value) ? new Optional<T>(enumParser(value)) : new Optional<T>();

  internal static List<T> ReadList<T>(this XmlReader reader, string wrapper, string elementName, Func<XmlReader, T> readElement)
  {
    var list = new List<T>();

    if (reader.IsEmptyElement)
      return list;

    reader.ReadStartElement(wrapper);
    while (reader.IsStartElement(elementName))
    {
      list.Add(readElement(reader));

      if (reader.NodeType == XmlNodeType.EndElement)
        continue; // At end of list, no need to read again

      _ = reader.Read();
    }
    reader.ReadEndElement();

    return list;
  }

  internal static void ProcessChildren(this XmlReader reader, string wrapper, Func<XmlReader, string, Action> getProcessAction)
  {
    if (reader.IsEmptyElement)
    {
      reader.ReadStartElement(wrapper);
      return;
    }

    reader.ReadStartElement(wrapper);
    while (reader.IsStartElement())
    {
      var elementName = reader.Name;
      var action = getProcessAction(reader, elementName);
      action();
    }
    reader.ReadEndElement();
  }

  internal static List<T> ProcessChildren<T>(this XmlReader reader, string wrapper, Func<XmlReader, string, T> getProcessAction)
  {
    var list = new List<T>();

    if (reader.IsEmptyElement)
    {
      reader.ReadStartElement(wrapper);
      return list;
    }

    reader.ReadStartElement(wrapper);
    while (reader.IsStartElement())
    {
      var elementName = reader.Name;
      var item = getProcessAction(reader, elementName);
      list.Add(item);
    }
    reader.ReadEndElement();

    return list;
  }

  internal static void SkipXmlDeclaration(this XmlReader reader)
  {
    if (reader.NodeType == XmlNodeType.XmlDeclaration)
      _ = reader.Read();
  }
}
