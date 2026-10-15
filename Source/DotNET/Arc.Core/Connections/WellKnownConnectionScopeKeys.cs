// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections;

/// <summary>
/// Holds the scope keys Arc puts on a <see cref="ConnectionRequest"/>.
/// </summary>
/// <remarks>
/// Scope keys are also the placeholders a template can use, written in braces: <c>{tenant}</c>.
/// </remarks>
public static class WellKnownConnectionScopeKeys
{
    /// <summary>
    /// The tenant the connection is for.
    /// </summary>
    public const string Tenant = "tenant";

    /// <summary>
    /// The placeholder for the <see cref="ConnectionPurpose"/> of the request, available to templates.
    /// </summary>
    public const string Purpose = "purpose";

    /// <summary>
    /// The placeholder for the storage kind of the request, available to templates.
    /// </summary>
    public const string Storage = "storage";
}
