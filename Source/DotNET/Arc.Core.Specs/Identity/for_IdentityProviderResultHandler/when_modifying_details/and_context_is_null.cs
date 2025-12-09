// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.Identity.for_IdentityProviderResultHandler.when_modifying_details;

public class and_context_is_null : given.an_identity_provider_result_handler
{
    void Establish()
    {
        _httpRequestContextAccessor.Current.Returns((IHttpRequestContext?)null);
    }

    async Task Because() => await _handler.ModifyDetails<object>(details => new { Modified = true });

    [Fact] void should_not_throw_exception() => true.ShouldBeTrue();
    [Fact] void should_not_call_identity_provider() => _identityProvider.DidNotReceive().Provide(Arg.Any<IdentityProviderContext>());
}
