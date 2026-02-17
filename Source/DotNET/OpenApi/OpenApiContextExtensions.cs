// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;

namespace Cratis.Arc.OpenApi;

/// <summary>
/// Provides utility methods for working with <see cref="OpenApiOperationTransformerContext"/>.
/// </summary>
public static class OpenApiContextExtensions
{
    /// <summary>
    /// Gets the <see cref="MethodInfo"/> from the <see cref="OpenApiOperationTransformerContext"/>.
    /// </summary>
    /// <param name="context">The <see cref="OpenApiOperationTransformerContext"/>.</param>
    /// <returns>The <see cref="MethodInfo"/> if available; otherwise, null.</returns>
    public static MethodInfo? GetMethodInfo(this OpenApiOperationTransformerContext context)
    {
        if (context.Description.ActionDescriptor is ControllerActionDescriptor controllerAction)
        {
            return controllerAction.MethodInfo;
        }

        return null;
    }
}
