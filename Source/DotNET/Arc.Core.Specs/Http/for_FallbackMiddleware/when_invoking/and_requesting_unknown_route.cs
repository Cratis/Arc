// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_FallbackMiddleware.when_invoking;

public class and_requesting_unknown_route : given.a_fallback_middleware
{
    bool _result;
    Task<System.Net.HttpListenerContext>? _contextTask;
    HttpClient? _client;
    System.Net.Http.HttpResponseMessage? _response;
    string? _content;
    string _testDirectory;
    const string FallbackContent = "<html><body>Fallback SPA</body></html>";

    void Establish()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"arc_specs_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);
        File.WriteAllText(Path.Combine(_testDirectory, "index.html"), FallbackContent);
        
        _middleware.ConfigureFallback("index.html", _testDirectory);

        _listener.Start();
        _client = new HttpClient();
    }

    async Task Because()
    {
        _contextTask = _listener.GetContextAsync();
        
        var responseTask = _client.GetAsync($"http://localhost:{_port}/app/unknown/route");
        
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
    [Fact] void should_return_fallback_content() => _content.ShouldEqual(FallbackContent);

    void Destroy()
    {
        _client?.Dispose();
        _response?.Dispose();
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }
}
