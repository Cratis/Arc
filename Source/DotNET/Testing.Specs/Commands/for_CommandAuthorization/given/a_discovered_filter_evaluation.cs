// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Commands.Filters;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cratis.Arc.Testing.Commands.for_CommandAuthorization.given;

public class a_discovered_filter_evaluation : an_evaluation
{
    void Establish()
    {
        // Keep AddCratisArcCore's real open-generic discovery registration, not a KnownInstancesOf fixture.
        Services.RemoveAll<IInstancesOf<ICommandFilter>>();
        var types = (ITypes)Services.Last(descriptor => descriptor.ServiceType == typeof(ITypes)).ImplementationInstance;
        types.FindMultiple<ICommandFilter>().Returns([typeof(AuthorizationFilter), typeof(AvailabilityFilter), typeof(DataAnnotationValidationFilter), typeof(FluentValidationFilter)]);
        Services.AddTransient<AuthorizationFilter>();
        Services.AddTransient<AvailabilityFilter>();
        Services.AddTransient<DataAnnotationValidationFilter>();
        Services.AddTransient<FluentValidationFilter>();
    }
}
