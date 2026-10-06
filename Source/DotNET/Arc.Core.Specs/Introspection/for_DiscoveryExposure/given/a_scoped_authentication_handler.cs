// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authentication;
using Cratis.Arc.Http;

namespace Cratis.Arc.Introspection.for_DiscoveryExposure.given;

public class a_scoped_authentication_handler : IAuthenticationHandler, IAsyncDisposable
{
    public bool Disposed { get; private set; }

    public Task<AuthenticationResult> HandleAuthentication(IHttpRequestContext context) => Task.FromResult(AuthenticationResult.Anonymous);

    public ValueTask DisposeAsync()
    {
        Disposed = true;
        return ValueTask.CompletedTask;
    }
}
