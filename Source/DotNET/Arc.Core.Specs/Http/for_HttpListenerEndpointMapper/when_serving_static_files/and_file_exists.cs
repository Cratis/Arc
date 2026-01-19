// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_HttpListenerEndpointMapper.when_serving_static_files;

public class and_file_exists : given.a_running_endpoint_mapper
{
    HttpResponseMessage _response;
    string _content;
    const string ExpectedContent = "<html><body>Hello World</body></html>";

    void Establish()
    {
        CreateTestFile("test.html", ExpectedContent);
        _endpointMapper.UseStaticFiles(new StaticFileOptions { FileSystemPath = _testDirectory });
        StartEndpointMapper();
    }

    async Task Because()
    {
        _response = await _httpClient.GetAsync("/test.html");
        _content = await _response.Content.ReadAsStringAsync();
    }

    [Fact] void should_return_success_status_code() => _response.IsSuccessStatusCode.ShouldBeTrue();
    [Fact] void should_return_file_content() => _content.ShouldEqual(ExpectedContent);
    [Fact] void should_set_correct_content_type() => _response.Content.Headers.ContentType?.MediaType.ShouldEqual("text/html");
}
