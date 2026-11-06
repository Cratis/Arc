// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization;

/// <summary>
/// Declares a scope that a verified caller's scope ceiling must hold to execute the operation.
/// Principals without a verified caller are not narrowed by this attribute.
/// </summary>
/// <remarks>
/// Every declared scope must be held. Declarations on a method combine with those on its declaring type.
/// </remarks>
/// <param name="scope">The scope the verified caller must hold.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequiresScopeAttribute(string scope) : Attribute
{
    /// <summary>
    /// Gets the scope the verified caller must hold.
    /// </summary>
    public string Scope { get; } = scope;
}
