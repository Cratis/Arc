// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.Identity.for_IdentityProviderResultHandler.given;

public class an_identity_provider_result_handler : Specification
{
    protected IHttpRequestContextAccessor _httpRequestContextAccessor;
    protected IHttpRequestContext _httpRequestContext;
    protected IProvideIdentityDetails _identityProvider;
    protected IdentityProviderResultHandler _handler;

    void Establish()
    {
        _httpRequestContext = Substitute.For<IHttpRequestContext>();
        _httpRequestContextAccessor = Substitute.For<IHttpRequestContextAccessor>();
        _httpRequestContextAccessor.Current.Returns(_httpRequestContext);

        _identityProvider = Substitute.For<IProvideIdentityDetails>();

        _handler = new(_httpRequestContextAccessor, _identityProvider);
    }
}
