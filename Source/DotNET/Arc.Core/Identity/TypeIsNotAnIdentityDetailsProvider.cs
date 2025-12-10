// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Exception that gets thrown when multiple identity details providers are found.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="TypeIsNotAnIdentityDetailsProvider"/>.
/// </remarks>
/// <param name="type">Violating type.</param>
public class TypeIsNotAnIdentityDetailsProvider(Type type) :
    Exception($"The type '{type.AssemblyQualifiedName}' is not an implementation of '{typeof(IProvideIdentityDetails).AssemblyQualifiedName}'")
{
    /// <summary>
    /// Throw if the type is not an identity details provider.
    /// </summary>
    /// <param name="type">Type to check.</param>
    /// <exception cref="TypeIsNotAnIdentityDetailsProvider">Thrown if the type does not implement <see cref="IProvideIdentityDetails"/>.</exception>
    public static void ThrowIfNotAnIdentityDetailsProvider(Type type)
    {
        if (!type.IsAssignableTo(typeof(IProvideIdentityDetails)))
        {
            throw new TypeIsNotAnIdentityDetailsProvider(type);
        }
    }
}