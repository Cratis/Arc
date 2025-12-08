// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.for_IdentityProviderResultHandler.when_generating_from_current_context;

public class and_claims_principal_is_null : given.an_identity_provider_result_handler
{
    IdentityProviderResult _result;

    void Establish()
    {
        _httpContext.User = null!;
    }

    async Task Because() => _result = await _handler.GenerateFromCurrentContext();

    [Fact] void should_return_anonymous_result() => _result.ShouldEqual(IdentityProviderResult.Anonymous);
}