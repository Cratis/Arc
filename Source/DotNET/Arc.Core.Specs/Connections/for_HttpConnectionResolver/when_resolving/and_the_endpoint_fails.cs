// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Arc.Connections.for_HttpConnectionResolver.when_resolving;

public class and_the_endpoint_fails : given.an_http_connection_resolver
{
    Exception? _error;

    void Establish() => _response = new(HttpStatusCode.Forbidden);

    async Task Because()
    {
        try
        {
            await _resolver.Resolve(_request);
        }
        catch (Exception ex)
        {
            _error = ex;
        }
    }

    [Fact] void should_fail_with_connection_resolution_failed() => _error.ShouldBeOfExactType<ConnectionResolutionFailed>();
    [Fact] void should_name_the_status() => _error!.Message.ShouldContain("403");
}
