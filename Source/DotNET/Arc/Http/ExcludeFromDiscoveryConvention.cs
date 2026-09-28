// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Cratis.Arc.AspNetCore.Http;

/// <summary>
/// Hides marked controller actions from MVC's API descriptions without changing their routes.
/// </summary>
internal sealed class ExcludeFromDiscoveryConvention : IApplicationModelConvention
{
    /// <inheritdoc/>
    public void Apply(ApplicationModel application)
    {
        foreach (var controller in application.Controllers)
        {
            var controllerExcluded = controller.ControllerType.IsDefined(typeof(ExcludeFromDiscoveryAttribute), true);
            foreach (var action in controller.Actions)
            {
                if (controllerExcluded || action.ActionMethod.IsDefined(typeof(ExcludeFromDiscoveryAttribute), true))
                {
                    action.ApiExplorer.IsVisible = false;
                }
            }
        }
    }
}
