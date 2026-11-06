// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Cratis.Arc.Authorization;

/// <summary>
/// Enforces a verified caller's scope ceiling on MVC controller actions before they run.
/// </summary>
/// <remarks>
/// Commands and queries are checked by the authorization evaluator. This filter applies the same <see cref="ScopeCeiling"/>
/// to the scopes declared with <see cref="RequiresScopeAttribute"/> on a controller action or its controller, and refuses
/// with 403 and without invoking the action. A principal without a verified caller is not narrowed.
/// </remarks>
internal sealed class ScopeCeilingFilter : IAuthorizationFilter
{
    /// <inheritdoc/>
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (context.ActionDescriptor is not ControllerActionDescriptor action)
        {
            return;
        }

        var required = AuthorizationDeclarations.RequiredScopesOf(action.ControllerTypeInfo, action.MethodInfo);
        if (!ScopeCeiling.Allows(VerifiedCallerPrincipal.PrincipalFor(context.HttpContext.Items, context.HttpContext.User), required))
        {
            context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
        }
    }
}
