// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.for_IdentityProviderResultHandler.when_writing;

public class when_context_is_null : given.an_identity_provider_result_handler
{
    IdentityProviderResult _identityProviderResult;

    void Establish()
    {
        _httpRequestContextAccessor.Current.Returns((Http.IHttpRequestContext?)null);
        _identityProviderResult = new IdentityProviderResult("user-123", "Test User", true, true, [], new { role = "Admin" });
    }

    async Task Because() => await _handler.Write(_identityProviderResult);

    [Fact] void should_not_throw() => true.ShouldBeTrue();
}
