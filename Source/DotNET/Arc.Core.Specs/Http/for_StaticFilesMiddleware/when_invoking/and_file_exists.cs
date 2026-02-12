// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_StaticFilesMiddleware.when_invoking;

public class and_file_exists : given.a_static_files_middleware
{
    bool _result;
    Task<System.Net.HttpListenerContext>? _contextTask;
    HttpClient? _client;
    System.Net.Http.HttpResponseMessage? _response;
    string? _content;
    const string ExpectedContent = "<html><body>Hello World</body></html>";

    void Establish()
    {
        var testFile = Path.Combine(_testDirectory, "test.html");
        File.WriteAllText(testFile, ExpectedContent);
        
        _middleware.AddConfiguration(new StaticFileOptions
        {
            FileSystemPath = _testDirectory
        });

        _listener.Start();
        _client = new HttpClient();
    }

    async Task Because()
    {
        _contextTask = _listener.GetContextAsync();
        
        var responseTask = _client.GetAsync($"http://localhost:{_port}/test.html");
        
        var context = await _contextTask;
        
        _result = await _middleware.InvokeAsync(context, ctx =>
        {
            ctx.Response.StatusCode = 404;
            ctx.Response.Close();
            return Task.FromResult(false);
        });

        _response = await responseTask;
        _content = await _response.Content.ReadAsStringAsync();
    }

    [Fact] void should_return_true() => _result.ShouldBeTrue();
    [Fact] void should_return_success_status() => _response!.IsSuccessStatusCode.ShouldBeTrue();
    [Fact] void should_return_file_content() => _content.ShouldEqual(ExpectedContent);
    [Fact] void should_set_correct_content_type() => _response!.Content.Headers.ContentType?.MediaType.ShouldEqual("text/html");

    void Destroy()
    {
        _client?.Dispose();
        _response?.Dispose();
    }
}
