// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_HttpListenerEndpointMapper.when_using_fallback_file;

public class and_requesting_unknown_route : given.a_running_endpoint_mapper
{
    HttpResponseMessage _response;
    string _content;
    const string ExpectedContent = "<html><body><div id='app'></div></body></html>";

    void Establish()
    {
        CreateTestFile("index.html", ExpectedContent);
        _endpointMapper.UseStaticFiles(new StaticFileOptions { FileSystemPath = _testDirectory });
        _endpointMapper.MapFallbackToFile("index.html");
        StartEndpointMapper();
    }

    async Task Because()
    {
        _response = await _httpClient.GetAsync("/some/client/route");
        _content = await _response.Content.ReadAsStringAsync();
    }

    [Fact] void should_return_success_status_code() => _response.IsSuccessStatusCode.ShouldBeTrue();
    [Fact] void should_return_fallback_file_content() => _content.ShouldEqual(ExpectedContent);
    [Fact] void should_set_correct_content_type() => _response.Content.Headers.ContentType?.MediaType.ShouldEqual("text/html");
}
