// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_HttpListenerEndpointMapper.when_using_fallback_file;

public class with_custom_fallback_file : given.a_running_endpoint_mapper
{
    HttpResponseMessage _response;
    string _content;
    const string ExpectedContent = "<html><body>Custom SPA Entry</body></html>";

    void Establish()
    {
        CreateTestFile("app.html", ExpectedContent);
        _endpointMapper.UseStaticFiles(new StaticFileOptions { FileSystemPath = _testDirectory });
        _endpointMapper.MapFallbackToFile("app.html");
        StartEndpointMapper();
    }

    async Task Because()
    {
        _response = await _httpClient.GetAsync("/dashboard/users/123");
        _content = await _response.Content.ReadAsStringAsync();
    }

    [Fact] void should_return_success_status_code() => _response.IsSuccessStatusCode.ShouldBeTrue();
    [Fact] void should_return_custom_fallback_file_content() => _content.ShouldEqual(ExpectedContent);
}
