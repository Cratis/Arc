// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.Identity.Callers.for_CredentialPresentationGate.given;

public class a_gate : Specification
{
    internal IHttpRequestContext _request;
    internal ICanReadForwardedIdentityRequest _raw;
    internal CredentialPresentation _result;
    internal readonly CredentialPresentationGate _gate = new();

    void Establish()
    {
        _request = for_VerifiedCallerResolver.given.a_resolver.NewRequest();
        _raw = (ICanReadForwardedIdentityRequest)_request;
    }
}
