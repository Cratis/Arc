// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_FallbackMiddleware.when_invoking;

public class and_fallback_file_does_not_exist : given.a_fallback_middleware
{
    bool _result;
    bool _nextCalled;
    Task<System.Net.HttpListenerContext>? _contextTask;
    HttpClient? _client;
    string _testDirectory;

    void Establish()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"arc_specs_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);
        
        _middleware.ConfigureFallback("nonexistent.html", _testDirectory);

        _listener.Start();
        _client = new HttpClient();
    }

    async Task Because()
    {
        _contextTask = _listener.GetContextAsync();
        
        var responseTask = _client.GetAsync($"http://localhost:{_port}/app/route");
        
        var context = await _contextTask;
        
        _nextCalled = false;
        _result = await _middleware.InvokeAsync(context, ctx =>
        {
            _nextCalled = true;
            ctx.Response.StatusCode = 404;
            ctx.Response.Close();
            return Task.FromResult(false);
        });

        await responseTask;
    }

    [Fact] void should_return_false() => _result.ShouldBeFalse();
    [Fact] void should_call_next() => _nextCalled.ShouldBeTrue();

    void Destroy()
    {
        _client?.Dispose();
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }
}
