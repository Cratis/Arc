// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EndpointGroups;

namespace Cratis.Arc.for_ArcApplicationExtensions.when_configuring;

[Collection("UsesCurrentDirectory")]
public class with_a_named_endpoint_group : given.a_host_without_discovery
{
    Exception? _error;

    void Establish()
    {
        _configureAccess = options => options.EndpointGroups.Assign("internal", 5001);
        BuildHost();
    }

    void Because() => _error = Catch.Exception(() => _app.UseCratisArc());

    [Fact] void should_refuse_to_activate() => _error.ShouldBeOfExactType<EndpointGroupsNotSupportedByHost>();
}
