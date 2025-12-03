// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator;

/// <summary>
/// Represents a provider for identity details types.
/// </summary>
public class IdentityDetailsTypesProvider
{
    const string ProvideIdentityDetailsGenericInterface = "Cratis.Arc.Identity.IProvideIdentityDetails`1";

    /// <summary>
    /// Initializes a new instance of the <see cref="IdentityDetailsTypesProvider"/> class.
    /// </summary>
    /// <param name="message">Logger to use for outputting messages.</param>
    public IdentityDetailsTypesProvider(Action<string> message)
    {
        message($"  Discover identity details types from {TypeExtensions.Assemblies.Count()} assemblies");

        var identityDetailsTypes = new List<Type>();

        foreach (var assembly in TypeExtensions.Assemblies)
        {
            foreach (var type in assembly.DefinedTypes)
            {
                if (type.IsClass && !type.IsAbstract)
                {
                    var detailsType = GetIdentityDetailsType(type);
                    if (detailsType is not null)
                    {
                        identityDetailsTypes.Add(detailsType);
                    }
                }
            }
        }

        IdentityDetailsTypes = identityDetailsTypes;
        message($"  Found {IdentityDetailsTypes.Count()} identity details types");
    }

    /// <summary>
    /// Gets all identity details types.
    /// </summary>
    public IEnumerable<Type> IdentityDetailsTypes { get; }

    static Type? GetIdentityDetailsType(Type type)
    {
        foreach (var @interface in type.GetInterfaces())
        {
            if (@interface.IsGenericType && @interface.FullName?.StartsWith(ProvideIdentityDetailsGenericInterface) == true)
            {
                return @interface.GetGenericArguments()[0];
            }
        }

        return null;
    }
}
