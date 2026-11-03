// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// Names the command property used to look up a read-model parameter.
/// </summary>
/// <param name="property">The declared property.</param>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
public sealed class LookUpByAttribute(string property) : Attribute
{
    /// <summary>
    /// Gets the declared property.
    /// </summary>
    public string Property { get; } = property;
}

#pragma warning disable SA1402 // The generic and non-generic lookup attributes share the same API name.

/// <summary>Looks up a read-model Handle parameter using the selected caller.</summary>
/// <typeparam name="TKeyProvider">A stateless provider with a public parameterless constructor.</typeparam>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
public sealed class LookUpByAttribute<TKeyProvider> : Attribute, ILookUpByKeyProvider
    where TKeyProvider : class, IReadModelKeyProvider, new()
{
    static readonly TKeyProvider _provider = new();

    /// <inheritdoc/>
    public Type ProviderType => typeof(TKeyProvider);

    /// <inheritdoc/>
    IReadModelKeyProvider ILookUpByKeyProvider.Provider => _provider;
}
