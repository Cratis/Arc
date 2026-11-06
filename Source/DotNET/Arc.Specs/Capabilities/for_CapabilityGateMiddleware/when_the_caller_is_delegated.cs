// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.Capabilities.for_CapabilityGateMiddleware;

public class when_the_caller_is_delegated : given.a_capability_gate
{
    void Establish()
    {
        var caller = _person with
        {
            PrincipalKind = CallerPrincipalKind.DelegatedActor,
            Actor = new("issuer", "subject"),
            Client = new("client", CallerClientAuthentication.Declared),
            GrantId = new("grant")
        };
        _verified.GetCurrent().Returns(new CallerResolution(caller, null));
    }
    Task Because() => Invoke();
    [Fact] void should_run_handler() => _nextRan.ShouldBeTrue();
    [Fact] void should_publish_actor() => _caller!.Actor.ShouldEqual(new CapabilityActor("issuer", "subject"));
    [Fact] void should_preserve_declared_client() => _caller!.Client.ShouldEqual(new CapabilityClient("client", false));
    [Fact] void should_publish_grant() => _caller!.GrantId.ShouldEqual("grant");
}
