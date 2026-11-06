// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerResolver.when_resolving;

public class with_the_target_changed_after_resolution : given.a_resolver
{
    CallerResolution _method;
    async Task Because()
    {
        await _resolver.GetCurrent();
        _raw.RawTarget.Returns("/other");
        _result = await _resolver.GetCurrent();
        _request = NewRequest();
        _requests.Current.Returns(_request);
        await _resolver.GetCurrent();
        _request.Method.Returns("POST");
        _method = await _resolver.GetCurrent();
    }
    [Fact] void should_refuse_a_target_change() => _result.Refusal.ShouldEqual(CallerRefusal.RequestChanged);
    [Fact] void should_refuse_a_method_change() => _method.Refusal.ShouldEqual(CallerRefusal.RequestChanged);
    [Fact] void should_have_no_callers() => (_result.Caller is null && _method.Caller is null).ShouldBeTrue();
    [Fact] void should_verify_once_per_request() => _source.Received(2).Verify(Arg.Any<CredentialPresentation>(), Arg.Any<CancellationToken>());
}
