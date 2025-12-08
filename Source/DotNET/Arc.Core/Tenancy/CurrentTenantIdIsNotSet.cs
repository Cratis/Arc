// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy;

/// <summary>
/// The exception that is thrown when the current tenant ID is not set.
/// </summary>
public class CurrentTenantIdIsNotSet() : Exception("Current tenant ID is not set")
{
    /// <summary>
    /// Check if the value is set, if not throw an exception.
    /// </summary>
    /// <param name="value">Value to check.</param>
    /// <exception cref="CurrentTenantIdIsNotSet">Thrown if the value is not set.</exception>
    public static void ThrowIfNotSet(TenantId? value)
    {
        if (value is null)
        {
            throw new CurrentTenantIdIsNotSet();
        }
    }
}
