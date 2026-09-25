// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.Authorization;

/// <summary>
/// Prevents legacy direct filter calls with no service provider from treating an advanced declaration as authorized.
/// </summary>
internal static class AuthorizationAttributeGuard
{
    /// <summary>
    /// Detects policy or scheme metadata that cannot be evaluated synchronously without an execution scope.
    /// </summary>
    /// <param name="target">The target or its declaring type.</param>
    /// <returns>Whether a scoped asynchronous evaluation is necessary.</returns>
    internal static bool RequiresScopedEvaluation(MemberInfo target) =>
        HasAdvancedSettings(target) || (target is MethodInfo { DeclaringType: { } type } && HasAdvancedSettings(type));

    /// <summary>
    /// Detects advanced metadata on any method an opaque query performer might expose.
    /// </summary>
    /// <param name="type">The performer's type.</param>
    /// <returns>Whether any member has advanced metadata.</returns>
    internal static bool HasAdvancedMethod(Type type) =>
        type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
            .Any(HasAdvancedSettings);

    static bool HasAdvancedSettings(MemberInfo target) => target.GetCustomAttributes(inherit: true).Any(attribute =>
        IsAuthorize(attribute.GetType()) &&
        (IsNonEmpty(attribute, "Policy") || IsNonEmpty(attribute, "AuthenticationSchemes")));

    static bool IsNonEmpty(object attribute, string property) =>
        attribute.GetType().GetProperty(property)?.GetValue(attribute) is string { Length: > 0 };

    static bool IsAuthorize(Type type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current == typeof(AuthorizeAttribute) || current.FullName == "Microsoft.AspNetCore.Authorization.AuthorizeAttribute")
            {
                return true;
            }
        }

        return false;
    }
}
