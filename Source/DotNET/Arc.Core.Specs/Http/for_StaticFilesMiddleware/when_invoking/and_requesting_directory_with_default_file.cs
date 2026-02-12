// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_StaticFilesMiddleware.when_invoking;

public class and_requesting_directory_with_default_file : given.a_static_files_middleware
{
    bool _result;
    Task<System.Net.HttpListenerContext>? _contextTask;
    HttpClient? _client;
    HttpResponseMessage? _response;
    string? _content;
    const string ExpectedContent = "Default index content";

    void Establish()
    {
        File.WriteAllText(Path.Combine(_testDirectory, "index.html"), ExpectedContent);

        _middleware.AddConfiguration(new StaticFileOptions
        {
            FileSystemPath = _testDirectory,
            ServeDefaultFiles = true,
            DefaultFileNames = ["index.html"]
        });

        _listener.Start();
        _client = new HttpClient();
    }

    async Task Because()
    {
        _contextTask = _listener.GetContextAsync();

        var responseTask = _client.GetAsync($"http://localhost:{_port}/");

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
    [Fact] void should_return_default_file_content() => _content.ShouldEqual(ExpectedContent);

    void Destroy()
    {
        _client?.Dispose();
        _response?.Dispose();
    }
}
