// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.Templates;

/// <summary>
/// Describes an enum.
/// </summary>
/// <param name="Type">Original type.</param>
/// <param name="Name">Name of the enum.</param>
/// <param name="Values">The values on the enum.</param>
/// <param name="TypesInvolved">Additional types involved.</param>
/// <param name="IsFlags">Whether the enum is decorated with <see cref="FlagsAttribute"/>. Defaults to <see langword="false"/>.</param>
/// <param name="Documentation">JSDoc documentation for the enum.</param>
public record EnumDescriptor(
    Type Type,
    string Name,
    IEnumerable<EnumMemberDescriptor> Values,
    IEnumerable<Type> TypesInvolved,
    bool IsFlags = false,
    string? Documentation = null) : IDescriptor
{
    /// <summary>
    /// Gets the expression combining all non-zero flag values with bitwise OR, e.g. <c>MyEnum.a | MyEnum.b</c>.
    /// Only meaningful when <see cref="IsFlags"/> is <see langword="true"/>.
    /// </summary>
    public string AllFlagsExpression => string.Join(
        " | ",
        Values
            .Where(v => Convert.ToInt64(v.Value) != 0)
            .Select(v => $"{Name}.{v.Name.ToCamelCase()}"));
}
