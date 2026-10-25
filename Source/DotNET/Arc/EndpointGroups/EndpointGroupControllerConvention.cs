// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EndpointGroups;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Cratis.Arc.AspNetCore.EndpointGroups;

/// <summary>
/// Puts the actions of MVC controllers in a named endpoint group into that group.
/// </summary>
/// <remarks>
/// <para>
/// A controller is in a named group when it, or its assembly, carries an <see cref="EndpointGroupAttribute"/>.
/// Every action of such a controller gets <see cref="EndpointGroupMetadata"/> on its endpoints, so
/// <see cref="EndpointGroupMatcherPolicy"/> serves it only on the ports assigned to the group, and is hidden from
/// API descriptions, so OpenAPI omits it.
/// </para>
/// <para>
/// Controllers in the default group are left as they are. A group without an assigned port is served on no port.
/// </para>
/// </remarks>
internal sealed class EndpointGroupControllerConvention : IApplicationModelConvention
{
    /// <inheritdoc/>
    public void Apply(ApplicationModel application)
    {
        foreach (var controller in application.Controllers)
        {
            var group = EndpointGroup.For(controller.ControllerType);
            if (group is null)
            {
                continue;
            }

            var metadata = new EndpointGroupMetadata(group);
            foreach (var action in controller.Actions)
            {
                action.ApiExplorer.IsVisible = false;
                foreach (var selector in action.Selectors)
                {
                    selector.EndpointMetadata.Add(metadata);
                }
            }
        }
    }
}
