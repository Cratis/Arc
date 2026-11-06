// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.Capabilities.for_CapabilityGateMiddleware;

public class when_the_caller_is_verified : given.a_capability_gate
{
    void Establish() => _verified.GetCurrent().Returns(new CallerResolution(_person, null));
    Task Because() => Invoke();
    [Fact] void should_run_handler() => _nextRan.ShouldBeTrue();
    [Fact] void should_publish_person() => _caller!.Person.ShouldEqual("person");
    [Fact] void should_publish_group() => _caller!.EndpointGroup.ShouldEqual("capabilities");
    [Fact] void should_set_the_user_to_the_verified_principal() => VerifiedCallerPrincipal.Carries(_context.User, _person).ShouldBeTrue();
    [Fact] void should_name_the_user_by_the_caller_subject() => _context.User.FindFirst("sub")!.Value.ShouldEqual("person");
    [Fact] void should_store_the_principal_for_arc() => _context.Items[VerifiedCallerPrincipal.ItemKey].ShouldEqual(_context.User);
    [Fact] void should_publish_protocol() => _caller!.Protocol.ShouldEqual("rest");
}
