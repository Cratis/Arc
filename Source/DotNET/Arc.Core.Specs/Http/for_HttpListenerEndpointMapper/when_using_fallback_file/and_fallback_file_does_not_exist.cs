// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Arc.Http.for_HttpListenerEndpointMapper.when_using_fallback_file;

public class and_fallback_file_does_not_exist : given.a_running_endpoint_mapper
{
    HttpResponseMessage _response;

    void Establish()
    {
        _endpointMapper.UseStaticFiles(new StaticFileOptions { FileSystemPath = _testDirectory });
        _endpointMapper.MapFallbackToFile("nonexistent.html");
        StartEndpointMapper();
    }

    async Task Because() => _response = await _httpClient.GetAsync("/some/route");

    [Fact] void should_return_not_found() => _response.StatusCode.ShouldEqual(HttpStatusCode.NotFound);
}
