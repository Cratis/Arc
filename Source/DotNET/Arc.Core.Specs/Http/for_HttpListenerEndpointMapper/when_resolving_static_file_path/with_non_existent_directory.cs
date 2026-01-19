// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_HttpListenerEndpointMapper.when_resolving_static_file_path;

public class with_non_existent_directory : given.a_running_endpoint_mapper
{
    HttpResponseMessage _response;

    void Establish()
    {
        _endpointMapper.UseStaticFiles(new StaticFileOptions { FileSystemPath = "non_existent_directory_12345" });
        StartEndpointMapper();
    }

    async Task Because() => _response = await _httpClient.GetAsync("/");

    [Fact] void should_return_not_found() => _response.StatusCode.ShouldEqual(System.Net.HttpStatusCode.NotFound);
}
