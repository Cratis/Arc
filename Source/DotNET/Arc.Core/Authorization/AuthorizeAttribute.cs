// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization;

/// <summary>
/// Specifies that the class or method that this attribute is applied to requires authorization.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
#pragma warning disable CA1813 // Avoid unsealed attributes
public class AuthorizeAttribute : Attribute
#pragma warning restore CA1813 // Avoid unsealed attributes
{
    /// <summary>
    /// Gets or sets the policy name that determines access to the resource.
    /// </summary>
    public string? Policy { get; set; }

    /// <summary>
    /// Gets or sets a comma delimited list of roles that are allowed to access the resource.
    /// </summary>
    public string? Roles { get; set; }

    /// <summary>
    /// Gets or sets a comma delimited list of schemes from which user information is constructed.
    /// </summary>
    public string? AuthenticationSchemes { get; set; }
}
