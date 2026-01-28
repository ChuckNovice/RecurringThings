namespace RecurringThings.MongoDB.Configuration;

using System;
using global::MongoDB.Bson;
using global::MongoDB.Bson.Serialization;
using global::MongoDB.Bson.Serialization.Serializers;

/// <summary>
/// A custom GUID serializer that stores GUIDs as strings in MongoDB.
/// </summary>
/// <remarks>
/// This serializer is used instead of <see cref="GuidSerializer"/> with <see cref="BsonType.String"/>
/// because the standard serializer throws "GuidRepresentation is Unspecified" in newer MongoDB drivers.
/// This implementation directly serializes/deserializes GUIDs as strings without the GuidRepresentation validation.
/// </remarks>
internal sealed class StringGuidSerializer : StructSerializerBase<Guid>
{
    /// <inheritdoc/>
    public override Guid Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var bsonType = context.Reader.GetCurrentBsonType();

        return bsonType switch
        {
            BsonType.String => Guid.Parse(context.Reader.ReadString()),
            BsonType.Binary => context.Reader.ReadBinaryData().ToGuid(),
            BsonType.Null => throw new FormatException("Cannot deserialize null to Guid"),
            _ => throw new FormatException($"Cannot deserialize BsonType {bsonType} to Guid")
        };
    }

    /// <inheritdoc/>
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Guid value)
    {
        context.Writer.WriteString(value.ToString());
    }
}
