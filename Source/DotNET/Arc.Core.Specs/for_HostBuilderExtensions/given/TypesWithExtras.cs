// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.for_HostBuilderExtensions.given;

/// <summary>
/// Represents an <see cref="ITypes"/> that reports everything the host's own universe does, plus extra types.
/// </summary>
/// <param name="inner">The host's own <see cref="ITypes"/>.</param>
/// <param name="extras">The extra types to report.</param>
public class TypesWithExtras(ITypes inner, Type[] extras) : ITypes
{
    /// <inheritdoc/>
    public TypeDiscoveryMode DiscoveryMode => inner.DiscoveryMode;

    /// <inheritdoc/>
    public IEnumerable<Assembly> Assemblies => inner.Assemblies;

    /// <inheritdoc/>
    public IEnumerable<Type> All => inner.All.Concat(extras).ToArray();

    /// <inheritdoc/>
    public Type FindSingle<T>() => inner.FindSingle<T>();

    /// <inheritdoc/>
    public IEnumerable<Type> FindMultiple<T>() => inner.FindMultiple<T>();

    /// <inheritdoc/>
    public Type FindSingle(Type type) => inner.FindSingle(type);

    /// <inheritdoc/>
    public IEnumerable<Type> FindMultiple(Type type) => inner.FindMultiple(type);

    /// <inheritdoc/>
    public Type FindTypeByFullName(string fullName) => inner.FindTypeByFullName(fullName);
}
