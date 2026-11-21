// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelServiceCollectionExtensions;

public class when_an_application_parameter_resolver_is_registered : given.an_application_parameter_resolver
{
    void Establish() => _services.AddScoped<IAsyncParameterDependencyResolver, AsyncApplicationResolver>();

    Task Because() => Resolve();

    [Fact] void should_await_the_application_resolver_for_the_handler_parameter() => _result.Arguments[0].ShouldBeSame(_dependency.Date);
}
