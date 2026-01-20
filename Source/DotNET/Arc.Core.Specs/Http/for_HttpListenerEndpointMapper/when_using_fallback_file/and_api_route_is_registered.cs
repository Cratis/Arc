// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_HttpListenerEndpointMapper.when_using_fallback_file;

public class and_api_route_is_registered : given.a_running_endpoint_mapper
{
    HttpResponseMessage _response;
    string _content;
    const string ApiResponse = "{\"message\":\"Hello from API\"}";
    const string FallbackContent = "<html><body>Fallback</body></html>";

    void Establish()
    {
        CreateTestFile("index.html", FallbackContent);
        _endpointMapper.UseStaticFiles(new StaticFileOptions { FileSystemPath = _testDirectory });
        _endpointMapper.MapFallbackToFile("index.html");
        _endpointMapper.MapGet("/api/greeting", async context =>
        {
            context.ContentType = "application/json";
            await context.WriteAsync(ApiResponse);
        });
        StartEndpointMapper();
    }

    async Task Because()
    {
        _response = await _httpClient.GetAsync("/api/greeting");
        _content = await _response.Content.ReadAsStringAsync();
    }

    [Fact] void should_return_success_status_code() => _response.IsSuccessStatusCode.ShouldBeTrue();
    [Fact] void should_return_api_response_not_fallback() => _content.ShouldEqual(ApiResponse);
    [Fact] void should_set_correct_content_type() => _response.Content.Headers.ContentType?.MediaType.ShouldEqual("application/json");
}
