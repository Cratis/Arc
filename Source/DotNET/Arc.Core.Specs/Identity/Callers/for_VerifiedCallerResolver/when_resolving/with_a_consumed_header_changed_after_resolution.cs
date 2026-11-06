// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerResolver.when_resolving;

public class with_a_consumed_header_changed_after_resolution : given.a_resolver
{
    CallerResolution _first;
    CallerResolution _unconsumed;
    async Task Because()
    {
        _source.Consumes("x-cratis-tenant-id").Returns(true);
        _raw.HeaderNames.Returns(["Authorization", "x-cratis-tenant-id", "x-request-id"]);
        _raw.GetHeaderValues("x-cratis-tenant-id").Returns(["tenant-test"]);
        _raw.GetHeaderValues("x-request-id").Returns(["first"]);
        _first = await _resolver.GetCurrent();
        _raw.GetHeaderValues("x-request-id").Returns(["second"]);
        _unconsumed = await _resolver.GetCurrent();
        _raw.GetHeaderValues("x-cratis-tenant-id").Returns(["other-tenant"]);
        _result = await _resolver.GetCurrent();
    }
    [Fact] void should_reuse_for_an_unconsumed_change() => ReferenceEquals(_first, _unconsumed).ShouldBeTrue();
    [Fact] void should_refuse_a_consumed_change() => _result.Refusal.ShouldEqual(CallerRefusal.RequestChanged);
    [Fact] void should_have_no_caller() => _result.Caller.ShouldBeNull();
    [Fact] void should_verify_once() => _source.Received(1).Verify(Arg.Any<CredentialPresentation>(), Arg.Any<CancellationToken>());
}
