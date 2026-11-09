// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.ProxyGenerator;

/// <summary>
/// Reads nullable-reference metadata straight from custom attribute data, so it also works for types loaded
/// through a MetadataLoadContext on every runtime (NullabilityInfoContext relies on runtime-specific Type members).
/// </summary>
static class Nullability
{
    const string NullableAttribute = "System.Runtime.CompilerServices.NullableAttribute";
    const string NullableContextAttribute = "System.Runtime.CompilerServices.NullableContextAttribute";
    const string AllowNullAttribute = "System.Diagnostics.CodeAnalysis.AllowNullAttribute";
    const string DisallowNullAttribute = "System.Diagnostics.CodeAnalysis.DisallowNullAttribute";
    const string MaybeNullAttribute = "System.Diagnostics.CodeAnalysis.MaybeNullAttribute";
    const string NotNullAttribute = "System.Diagnostics.CodeAnalysis.NotNullAttribute";

    /// <summary>
    /// Check if a reference-typed property accepts (setter) or returns (getter only) null.
    /// </summary>
    /// <param name="property">Property to check.</param>
    /// <returns>True if nullable.</returns>
    public static bool IsNullable(PropertyInfo property)
    {
        var attributes = Safe(property.GetCustomAttributesData);
        var write = property.SetMethod is not null;

        // Annotation attributes are emitted on the accessor (value parameter or return value) as well as possibly on the property.
        var annotations = write
            ? attributes.Concat(Safe(property.SetMethod!.GetParameters()[^1].GetCustomAttributesData)).ToList()
            : attributes.Concat(property.GetMethod is { } getter ? Safe(getter.ReturnParameter.GetCustomAttributesData) : []).ToList();
        if (write && Has(annotations, AllowNullAttribute))
        {
            return true;
        }
        if (write && Has(annotations, DisallowNullAttribute))
        {
            return false;
        }
        if (!write && Has(annotations, MaybeNullAttribute))
        {
            return true;
        }
        if (!write && Has(annotations, NotNullAttribute))
        {
            return false;
        }

        var flag = FlagOf(attributes, NullableAttribute) ?? Context(attributes, property.DeclaringType);
        return flag == 2;
    }

    /// <summary>
    /// Check if a reference-typed parameter accepts null.
    /// </summary>
    /// <param name="parameter">Parameter to check.</param>
    /// <returns>True if nullable.</returns>
    public static bool IsNullable(ParameterInfo parameter)
    {
        var attributes = Safe(parameter.GetCustomAttributesData);
        if (Has(attributes, AllowNullAttribute))
        {
            return true;
        }
        if (Has(attributes, DisallowNullAttribute))
        {
            return false;
        }

        var flag = FlagOf(attributes, NullableAttribute);
        if (flag is null && parameter.Member is MemberInfo member)
        {
            var memberAttributes = Safe(member.GetCustomAttributesData);
            flag = Context(memberAttributes, member.DeclaringType);
        }
        return flag == 2;
    }

    static byte? Context(IList<CustomAttributeData> memberAttributes, Type? declaringType)
    {
        var flag = FlagOf(memberAttributes, NullableContextAttribute);
        var type = declaringType;
        while (flag is null && type is not null)
        {
            flag = FlagOf(Safe(() => type.GetCustomAttributesData()), NullableContextAttribute);
            type = type.DeclaringType;
        }
        return flag;
    }

    static bool Has(IList<CustomAttributeData> attributes, string name) =>
        attributes.Any(_ => _.AttributeType.FullName == name);

    static byte? FlagOf(IList<CustomAttributeData> attributes, string name)
    {
        var attribute = attributes.FirstOrDefault(_ => _.AttributeType.FullName == name);
        if (attribute is null || attribute.ConstructorArguments.Count == 0)
        {
            return null;
        }

        var value = attribute.ConstructorArguments[0].Value;
        return value switch
        {
            byte single => single,
            IReadOnlyCollection<CustomAttributeTypedArgument> { Count: > 0 } array when array.First().Value is byte first => first,
            _ => null
        };
    }

    static IList<CustomAttributeData> Safe(Func<IList<CustomAttributeData>> get)
    {
        try
        {
            return get();
        }
        catch (Exception ex) when (ex is FileNotFoundException or FileLoadException or NotSupportedException)
        {
            return [];
        }
    }
}
