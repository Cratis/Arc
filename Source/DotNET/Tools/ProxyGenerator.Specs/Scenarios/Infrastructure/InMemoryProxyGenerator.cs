// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

/// <summary>
/// Generates TypeScript proxies in memory for testing.
/// </summary>
public static class InMemoryProxyGenerator
{
    /// <summary>
    /// Generates a TypeScript command proxy from a descriptor.
    /// </summary>
    /// <param name="descriptor">The command descriptor.</param>
    /// <returns>The generated TypeScript code.</returns>
    public static string GenerateCommand(CommandDescriptor descriptor)
    {
        return TemplateTypes.Command(descriptor);
    }

    /// <summary>
    /// Generates a TypeScript query proxy from a descriptor.
    /// </summary>
    /// <param name="descriptor">The query descriptor.</param>
    /// <returns>The generated TypeScript code.</returns>
    public static string GenerateQuery(QueryDescriptor descriptor)
    {
        return descriptor.IsObservable
            ? TemplateTypes.ObservableQuery(descriptor)
            : TemplateTypes.Query(descriptor);
    }

    /// <summary>
    /// Generates a TypeScript type from a descriptor.
    /// </summary>
    /// <param name="descriptor">The type descriptor.</param>
    /// <returns>The generated TypeScript code.</returns>
    public static string GenerateType(TypeDescriptor descriptor)
    {
        return TemplateTypes.Type(descriptor);
    }

    /// <summary>
    /// Generates a TypeScript enum from a descriptor.
    /// </summary>
    /// <param name="descriptor">The enum descriptor.</param>
    /// <returns>The generated TypeScript code.</returns>
    public static string GenerateEnum(EnumDescriptor descriptor)
    {
        return TemplateTypes.Enum(descriptor);
    }

    /// <summary>
    /// Generates a TypeScript flags enum from a descriptor, including an <c>allXxx</c> constant.
    /// </summary>
    /// <param name="descriptor">The flags enum descriptor.</param>
    /// <returns>The generated TypeScript code.</returns>
    public static string GenerateFlagsEnum(EnumDescriptor descriptor)
    {
        return TemplateTypes.FlagsEnum(descriptor);
    }
}
