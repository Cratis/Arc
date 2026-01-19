// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_HttpListenerEndpointMapper.when_serving_static_files;

public class and_requesting_directory_with_default_file : given.a_running_endpoint_mapper
{
    HttpResponseMessage _response;
    string _content;
    const string ExpectedContent = "<html><body>Index Page</body></html>";

    void Establish()
    {
        CreateTestFile("index.html", ExpectedContent);
        _endpointMapper.UseStaticFiles(new StaticFileOptions
        {
            FileSystemPath = _testDirectory,
            ServeDefaultFiles = true
        });
        StartEndpointMapper();
    }

    async Task Because()
    {
        _response = await _httpClient.GetAsync("/");
        _content = await _response.Content.ReadAsStringAsync();
    }

    [Fact] void should_return_success_status_code() => _response.IsSuccessStatusCode.ShouldBeTrue();
    [Fact] void should_return_index_file_content() => _content.ShouldEqual(ExpectedContent);
}
