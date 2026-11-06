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
    [Fact] void should_publish_protocol() => _caller!.Protocol.ShouldEqual("rest");
}
