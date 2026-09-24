// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.Queries;

/// <summary>
/// Exposes the method whose authorization declaration applies to a query performer.
/// </summary>
public interface IAuthorizationQueryTarget
{
    /// <summary>
    /// Gets the query method used for authorization.
    /// </summary>
    MethodInfo AuthorizationMethod { get; }
}
