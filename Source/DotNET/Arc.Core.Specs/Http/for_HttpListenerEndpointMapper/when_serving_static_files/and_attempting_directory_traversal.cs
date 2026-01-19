// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Arc.Http.for_HttpListenerEndpointMapper.when_serving_static_files;

public class and_attempting_directory_traversal : given.a_running_endpoint_mapper
{
    HttpResponseMessage _response;

    void Establish()
    {
        CreateTestFile("safe.html", "Safe content");
        _endpointMapper.UseStaticFiles(new StaticFileOptions { FileSystemPath = _testDirectory });
        StartEndpointMapper();
    }

    async Task Because() => _response = await _httpClient.GetAsync("/../../../etc/passwd");

    [Fact] void should_return_not_found() => _response.StatusCode.ShouldEqual(HttpStatusCode.NotFound);
}
