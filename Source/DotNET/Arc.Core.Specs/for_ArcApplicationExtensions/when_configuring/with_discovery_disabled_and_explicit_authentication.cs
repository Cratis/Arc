// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.for_ArcApplicationExtensions.when_configuring;

[Collection("UsesCurrentDirectory")]
public class with_discovery_disabled_and_explicit_authentication : given.a_host_without_discovery
{
    void Establish()
    {
        _configureAccess = options => options.Introspection.RequireAuthentication = true;
        BuildHost();
    }

    void Because() => _app.UseCratisArc();

    [Fact] void should_activate_arc() => _app.IsCratisArcConfigured.ShouldBeTrue();
    [Fact] void should_not_map_discovery() => DiscoveryRoutes.ShouldBeEmpty();
    [Fact] void should_keep_current_caller_identity() => HasCurrentCallerIdentity.ShouldBeTrue();
    [Fact] void should_not_check_authentication_handlers() => _ = _authentication.DidNotReceive().HasHandlers;
}
