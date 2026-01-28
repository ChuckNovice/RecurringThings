namespace RecurringThings.MongoDB.Configuration;

using System;
using global::MongoDB.Bson.Serialization;
using global::MongoDB.Bson.Serialization.Conventions;
using global::MongoDB.Bson.Serialization.Serializers;

/// <summary>
/// Convention that serializes all Guid properties as strings instead of binary.
/// </summary>
/// <remarks>
/// <para>
/// This convention applies to both <see cref="Guid"/> and <see cref="Nullable{Guid}"/> properties,
/// ensuring consistent string representation without requiring attributes on each property.
/// </para>
/// <para>
/// Uses <see cref="StringGuidSerializer"/> instead of the built-in GuidSerializer to avoid
/// the "GuidRepresentation is Unspecified" error in newer MongoDB driver versions.
/// </para>
/// </remarks>
internal sealed class GuidStringRepresentationConvention : IMemberMapConvention
{
    private static readonly StringGuidSerializer GuidSerializer = new();

    /// <summary>
    /// Gets the name of this convention.
    /// </summary>
    public string Name => "GuidStringRepresentation";

    /// <summary>
    /// Applies the convention to a member map.
    /// </summary>
    /// <param name="memberMap">The member map to apply the convention to.</param>
    public void Apply(BsonMemberMap memberMap)
    {
        ArgumentNullException.ThrowIfNull(memberMap);

        if (memberMap.MemberType == typeof(Guid))
        {
            memberMap.SetSerializer(GuidSerializer);
        }
        else if (memberMap.MemberType == typeof(Guid?))
        {
            memberMap.SetSerializer(new NullableSerializer<Guid>(GuidSerializer));
        }
    }
}
