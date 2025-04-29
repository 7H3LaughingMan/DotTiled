using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;

namespace DotTiled.Serialization.Tmx;

public abstract partial class TmxReaderBase
{
  internal List<IProperty> ReadProperties()
  {
    if (!_reader.IsStartElement("properties"))
      return [];

    return _reader.ReadList("properties", "property", (r) =>
    {
      var name = r.GetRequiredAttribute("name");
      var type = r.GetOptionalAttributeEnum<PropertyType>("type", Helpers.CreateMapper<PropertyType>(
        s => throw new InvalidOperationException($"Unknown property type '{s}'"),
        ("string", PropertyType.String),
        ("int", PropertyType.Int),
        ("float", PropertyType.Float),
        ("bool", PropertyType.Bool),
        ("color", PropertyType.Color),
        ("file", PropertyType.File),
        ("object", PropertyType.Object),
        ("class", PropertyType.Class)
      )).GetValueOr(PropertyType.String);

      var propertyType = r.GetOptionalAttribute("propertytype");
      if (propertyType.HasValue)
        return ReadPropertyWithCustomType();

      return type switch
      {
        PropertyType.String => ReadStringProperty(name),
        PropertyType.Int => new IntProperty { Name = name, Value = r.GetRequiredAttributeInt32("value") },
        PropertyType.Float => new FloatProperty { Name = name, Value = r.GetRequiredAttributeSingle("value") },
        PropertyType.Bool => new BoolProperty { Name = name, Value = r.GetRequiredAttributeBoolean("value") },
        PropertyType.Color => new ColorProperty { Name = name, Value = r.GetRequiredAttributeParseable<TiledColor>("value", s => s == "" ? default : TiledColor.Parse(s, CultureInfo.InvariantCulture)) },
        PropertyType.File => new FileProperty { Name = name, Value = r.GetRequiredAttribute("value") },
        PropertyType.Object => new ObjectProperty { Name = name, Value = r.GetRequiredAttributeUInt32("value") },
        PropertyType.Class => throw new InvalidOperationException("Class property must have a property type"),
        PropertyType.Enum => throw new InvalidOperationException("Enum property must have a property type"),
        _ => throw new InvalidOperationException($"Invalid property type '{Enum.GetName(type)}'")
      };
    });
  }

  internal StringProperty ReadStringProperty(string name)
  {
    var valueAttrib = _reader.GetOptionalAttribute("value");
    if (valueAttrib.HasValue)
    {
      return new StringProperty { Name = name, Value = valueAttrib.Value };
    }

    if (!_reader.IsEmptyElement)
    {
      _reader.ReadStartElement("property");
      var value = _reader.ReadContentAsString();
      _reader.ReadEndElement();
      return new StringProperty { Name = name, Value = value };
    }

    return new StringProperty { Name = name, Value = string.Empty };
  }

  internal IProperty ReadPropertyWithCustomType()
  {
    var isClass = _reader.GetOptionalAttribute("type") == "class";
    if (isClass)
    {
      return ReadClassProperty();
    }

    return ReadEnumProperty();
  }

  internal ClassProperty ReadClassProperty()
  {
    var name = _reader.GetRequiredAttribute("name");
    var propertyType = _reader.GetRequiredAttribute("propertytype");
    var customTypeDef = _customTypeResolver(propertyType);

    // If the custom class definition is not found,
    // we assume an empty class definition.
    if (!customTypeDef.HasValue)
    {
      if (!_reader.IsEmptyElement)
      {
        _reader.ReadStartElement("property");
        var props = ReadProperties();
        _reader.ReadEndElement();
        return new ClassProperty { Name = name, PropertyType = propertyType, Value = props };
      }

      return new ClassProperty { Name = name, PropertyType = propertyType, Value = [] };
    }

    if (customTypeDef.Value is not CustomClassDefinition ccd)
      throw new XmlException($"Custom type {propertyType} is not a class.");

    var propsInType = Helpers.CreateInstanceOfCustomClass(ccd, _customTypeResolver);
    if (!_reader.IsEmptyElement)
    {
      _reader.ReadStartElement("property");
      var props = ReadProperties();
      var mergedProps = Helpers.MergeProperties(propsInType, props);
      _reader.ReadEndElement();
      return new ClassProperty { Name = name, PropertyType = propertyType, Value = mergedProps };
    }

    return new ClassProperty { Name = name, PropertyType = propertyType, Value = propsInType };
  }

  internal IProperty ReadEnumProperty()
  {
    var name = _reader.GetRequiredAttribute("name");
    var propertyType = _reader.GetRequiredAttribute("propertytype");
    var typeInXml = _reader.GetOptionalAttributeEnum<PropertyType>("type", Helpers.CreateMapper<PropertyType>(
      s => throw new InvalidOperationException($"Invalid property type '{s}'"),
      ("string", PropertyType.String),
      ("int", PropertyType.Int)
    )).GetValueOr(PropertyType.String);
    var customTypeDef = _customTypeResolver(propertyType);

    // If the custom enum definition is not found,
    // we assume an empty enum definition.
    if (!customTypeDef.HasValue)
    {
#pragma warning disable IDE0072 // Add missing cases
      return typeInXml switch
      {
        PropertyType.String => new StringProperty { Name = name, Value = _reader.GetRequiredAttribute("value") },
        PropertyType.Int => new IntProperty { Name = name, Value = _reader.GetRequiredAttributeInt32("value") },
        _ => throw new InvalidOperationException($"Invalid property type '{Enum.GetName(typeInXml)}'")
      };
#pragma warning restore IDE0072 // Add missing cases
    }

    if (customTypeDef.Value is not CustomEnumDefinition ced)
      throw new XmlException($"Custom defined type {propertyType} is not an enum.");

    if (ced.StorageType == CustomEnumStorageType.String)
    {
      var value = _reader.GetRequiredAttribute("value");
      if (value.Contains(',') && !ced.ValueAsFlags)
        throw new XmlException("Enum value must not contain ',' if not ValueAsFlags is set to true.");

      if (ced.ValueAsFlags)
      {
        var values = value.Split(',').Select(v => v.Trim()).ToHashSet();
        return new EnumProperty { Name = name, PropertyType = propertyType, Value = values };
      }
      else
      {
        return new EnumProperty { Name = name, PropertyType = propertyType, Value = new HashSet<string> { value } };
      }
    }
    else if (ced.StorageType == CustomEnumStorageType.Int)
    {
      var value = _reader.GetRequiredAttributeInt32("value");
      if (ced.ValueAsFlags)
      {
        var allValues = ced.Values;
        var enumValues = new HashSet<string>();
        for (var i = 0; i < allValues.Count; i++)
        {
          var mask = 1 << i;
          if ((value & mask) == mask)
          {
            var enumValue = allValues[i];
            _ = enumValues.Add(enumValue);
          }
        }
        return new EnumProperty { Name = name, PropertyType = propertyType, Value = enumValues };
      }
      else
      {
        var allValues = ced.Values;
        var enumValue = allValues[value];
        return new EnumProperty { Name = name, PropertyType = propertyType, Value = new HashSet<string> { enumValue } };
      }
    }

    throw new XmlException($"Unable to read enum property {name} with type {propertyType}");
  }
}
