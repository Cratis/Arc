// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.ProxyGenerator.ModelBound;

/// <summary>
/// Extension methods for resolving the endpoint group of a command or read model type from its metadata.
/// </summary>
public static class EndpointGroupExtensions
{
    const string EndpointGroupAttributeName = "Cratis.Arc.EndpointGroupAttribute";

    /// <summary>
    /// Gets the name of the endpoint group a type belongs to. An attribute on the type wins over one on its assembly.
    /// </summary>
    /// <param name="type">The command or read model type.</param>
    /// <returns>The name of the group, or <see langword="null"/> for the default group.</returns>
    public static string? GetEndpointGroup(this Type type) =>
        NameFrom(type.GetCustomAttributesData()) ?? NameFrom(type.Assembly.GetCustomAttributesData());

    /// <summary>
    /// Checks whether a type belongs to the endpoint group proxies are generated for.
    /// </summary>
    /// <param name="type">The command or read model type.</param>
    /// <param name="endpointGroup">The selected group, or <see langword="null"/> for the default group.</param>
    /// <returns>True if the type is in the selected group, false if not.</returns>
    public static bool IsInEndpointGroup(this Type type, string? endpointGroup) =>
        string.Equals(type.GetEndpointGroup(), endpointGroup, StringComparison.Ordinal);

    /// <summary>
    /// Gets a query method's group, falling back to its read model and assembly.
    /// </summary>
    /// <param name="method">The query method.</param>
    /// <param name="owner">The read model type.</param>
    /// <returns>The group name, or null for the default group.</returns>
    public static string? GetEndpointGroup(this MethodInfo method, Type owner) =>
        NameFrom(method.GetCustomAttributesData()) ?? owner.GetEndpointGroup();

    /// <summary>
    /// Checks whether a query method belongs to the group proxies are generated for.
    /// </summary>
    /// <param name="method">The query method.</param>
    /// <param name="owner">The read model type.</param>
    /// <param name="endpointGroup">The selected group, or null for the default group.</param>
    /// <returns>True if the query belongs to the selected group.</returns>
    public static bool IsInEndpointGroup(this MethodInfo method, Type owner, string? endpointGroup) =>
        string.Equals(method.GetEndpointGroup(owner), endpointGroup, StringComparison.Ordinal);

    static string? NameFrom(IEnumerable<CustomAttributeData> attributes) =>
        attributes
            .FirstOrDefault(_ => _.AttributeType.FullName == EndpointGroupAttributeName)?
            .ConstructorArguments.FirstOrDefault().Value as string;
}
