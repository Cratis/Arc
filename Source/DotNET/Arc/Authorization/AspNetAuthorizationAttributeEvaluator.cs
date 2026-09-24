// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.Authorization;

/// <summary>
/// Represents an implementation of <see cref="IAuthorizationAttributeEvaluator"/> that checks for ASP.NET Core's
/// <see cref="Microsoft.AspNetCore.Authorization.AuthorizeAttribute"/>.
/// </summary>
/// <remarks>
/// The ASP.NET Core attribute type is named in full on purpose: in this namespace an unqualified
/// <c>AuthorizeAttribute</c> resolves to Arc's own attribute, not ASP.NET Core's.
/// </remarks>
public class AspNetAuthorizationAttributeEvaluator : IAuthorizationAttributeEvaluator
{
    /// <inheritdoc/>
    public (bool HasAuthorize, string? Roles)? GetAuthorizationInfo(Type type) => FirstOf(AttributesOn(type));

    /// <inheritdoc/>
    public (bool HasAuthorize, string? Roles)? GetAuthorizationInfo(MethodInfo method) => FirstOf(AttributesOn(method));

    /// <inheritdoc/>
    public IEnumerable<AuthorizationRequirement> GetAuthorizationRequirements(Type type) =>
        AttributesOn(type).Select(attribute => AuthorizationRequirement.FromAttribute(attribute.Roles, attribute.Policy, attribute.AuthenticationSchemes));

    /// <inheritdoc/>
    public IEnumerable<AuthorizationRequirement> GetAuthorizationRequirements(MethodInfo method) =>
        AttributesOn(method).Select(attribute => AuthorizationRequirement.FromAttribute(attribute.Roles, attribute.Policy, attribute.AuthenticationSchemes));

    static IEnumerable<Microsoft.AspNetCore.Authorization.AuthorizeAttribute> AttributesOn(MemberInfo member) =>
        member.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), inherit: true)
            .OfType<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>();

    static (bool HasAuthorize, string? Roles)? FirstOf(IEnumerable<Microsoft.AspNetCore.Authorization.AuthorizeAttribute> attributes) =>
        attributes.FirstOrDefault() is { } attribute ? (true, attribute.Roles) : null;
}
