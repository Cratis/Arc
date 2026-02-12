// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_StaticFilesMiddleware.when_invoking;

public class and_file_is_in_subdirectory : given.a_static_files_middleware
{
    bool _result;
    Task<System.Net.HttpListenerContext>? _contextTask;
    HttpClient? _client;
    HttpResponseMessage? _response;
    string? _content;
    const string ExpectedContent = "Subdirectory content";

    void Establish()
    {
        var subDir = Path.Combine(_testDirectory, "sub");
        Directory.CreateDirectory(subDir);
        File.WriteAllText(Path.Combine(subDir, "page.html"), ExpectedContent);

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

        var responseTask = _client.GetAsync($"http://localhost:{_port}/sub/page.html");

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
    [Fact] void should_return_file_content() => _content.ShouldEqual(ExpectedContent);

    void Destroy()
    {
        _client?.Dispose();
        _response?.Dispose();
    }
}
