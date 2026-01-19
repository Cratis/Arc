// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_HttpListenerEndpointMapper.when_using_fallback_file;

public class and_static_file_exists : given.a_running_endpoint_mapper
{
    HttpResponseMessage _response;
    string _content;
    const string StaticFileContent = "body { color: blue; }";
    const string FallbackContent = "<html><body>Fallback</body></html>";

    void Establish()
    {
        CreateTestFile("styles.css", StaticFileContent);
        CreateTestFile("index.html", FallbackContent);
        _endpointMapper.UseStaticFiles(new StaticFileOptions { FileSystemPath = _testDirectory });
        _endpointMapper.MapFallbackToFile("index.html");
        StartEndpointMapper();
    }

    async Task Because()
    {
        _response = await _httpClient.GetAsync("/styles.css");
        _content = await _response.Content.ReadAsStringAsync();
    }

    [Fact] void should_return_success_status_code() => _response.IsSuccessStatusCode.ShouldBeTrue();
    [Fact] void should_return_static_file_content_not_fallback() => _content.ShouldEqual(StaticFileContent);
    [Fact] void should_set_correct_content_type() => _response.Content.Headers.ContentType?.MediaType.ShouldEqual("text/css");
}
