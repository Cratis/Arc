// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace Cratis.Arc.Queries.ModelBound;

/// <summary>
/// Probes metadata-only markers without resolving unrelated attribute assemblies.
/// </summary>
internal static class MetadataQueryAttributes
{
    /// <summary>
    /// Finds an exact marker using only the member's own metadata image before reading its values.
    /// </summary>
    /// <param name="member">The metadata-only member.</param>
    /// <param name="fullName">The full attribute type name.</param>
    /// <param name="matchFullName">Whether to match a full name rather than the legacy simple-name predicate.</param>
    /// <returns>The matching attribute, or null.</returns>
    internal static CustomAttributeData? Find(MemberInfo member, string fullName, bool matchFullName = true)
    {
        if (string.IsNullOrEmpty(member.Module.Assembly.Location))
        {
            // In-memory metadata has no physical image to probe; retain reflection behavior for that adapter.
            return member.GetCustomAttributesData().FirstOrDefault(_ =>
                (matchFullName ? _.AttributeType.FullName : _.AttributeType.Name) == fullName);
        }

        using var stream = File.OpenRead(member.Module.FullyQualifiedName);
        using var image = new PEReader(stream, PEStreamOptions.PrefetchMetadata);
        var reader = image.GetMetadataReader();
        var index = 0;
        foreach (var handle in reader.GetCustomAttributes(MetadataTokens.EntityHandle(member.MetadataToken)))
        {
            var name = TypeName(reader, AttributeType(reader, handle));
            if (Matches(name, fullName, matchFullName))
            {
                // MetadataLoadContext exposes custom attributes in the image's metadata order. Only the
                // matching entry is resolved; unrelated constructors and AttributeType values stay untouched.
                return member.GetCustomAttributesData()[index];
            }

            index++;
        }

        return null;
    }

    static EntityHandle AttributeType(MetadataReader reader, CustomAttributeHandle attribute)
    {
        var constructor = reader.GetCustomAttribute(attribute).Constructor;
        return constructor.Kind switch
        {
            HandleKind.MemberReference => reader.GetMemberReference((MemberReferenceHandle)constructor).Parent,
            HandleKind.MethodDefinition => reader.GetMethodDefinition((MethodDefinitionHandle)constructor).GetDeclaringType(),
            _ => default
        };
    }

    static bool Matches(string? name, string expected, bool fullName)
    {
        if (name is null)
        {
            return false;
        }

        var candidate = fullName ? name : name[(name.LastIndexOfAny(['.', '+']) + 1)..];
        return candidate == expected;
    }

    static string? TypeName(MetadataReader reader, EntityHandle handle) => handle.Kind switch
    {
        HandleKind.TypeReference => ReferenceName(reader, reader.GetTypeReference((TypeReferenceHandle)handle)),
        HandleKind.TypeDefinition => DefinitionName(reader, reader.GetTypeDefinition((TypeDefinitionHandle)handle)),
        _ => null
    };

    static string ReferenceName(MetadataReader reader, TypeReference type) => type.ResolutionScope.Kind == HandleKind.TypeReference
        ? $"{TypeName(reader, type.ResolutionScope)}+{reader.GetString(type.Name)}"
        : QualifiedName(reader.GetString(type.Namespace), reader.GetString(type.Name));

    static string DefinitionName(MetadataReader reader, TypeDefinition type) => type.GetDeclaringType().IsNil
        ? QualifiedName(reader.GetString(type.Namespace), reader.GetString(type.Name))
        : $"{TypeName(reader, type.GetDeclaringType())}+{reader.GetString(type.Name)}";

    static string QualifiedName(string @namespace, string name) => @namespace.Length == 0 ? name : $"{@namespace}.{name}";
}
