// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerResolver.when_resolving;

public class without_a_request : given.a_resolver
{
    void Establish() => _requests.Current.Returns((Http.IHttpRequestContext?)null);
    async Task Because() => _result = await _resolver.GetCurrent();

    [Fact] void should_have_no_credential() => _result.Refusal.ShouldEqual(CallerRefusal.NoCredential);
    [Fact] void should_not_construct_a_caller() => _result.Caller.ShouldBeNull();
}
