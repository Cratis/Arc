// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Cratis.Arc.Testing.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.Commands.for_CommandAuthorization;

public class when_a_request_is_active : given.an_evaluation
{
    IHttpRequestContextAccessor _requests;
    void Establish()
    {
        _requests = Provider.GetRequiredService<IHttpRequestContextAccessor>();
        _requests.Current = Substitute.For<IHttpRequestContext>();
    }
    async Task Because() => Result = await CommandAuthorization.For<given.RoleCommand>(Provider, Principal);
    [Fact] void should_be_unsupported() => Result.ShouldBeUnsupported();
    [Fact] void should_require_a_host() => Result.ReasonCode.ShouldEqual("host-required");
    void Destroy() => _requests.Current = null;
}
