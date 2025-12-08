// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.for_IdentityProviderEndpoint.given;

public abstract class a_valid_identity_request : an_identity_provider_endpoint
{
    protected string _identityId = "123";
    protected string _identityName = "Test User";
    protected IdentityProviderResult _identityProviderResult = new(new IdentityId("123"), new IdentityName("Test User"), true, true, "Hello world");

    void Establish()
    {
        _identityProviderResultHandler.GenerateFromCurrentContext().Returns(_identityProviderResult);
    }
}