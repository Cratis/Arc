// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_FallbackMiddleware.when_invoking;

public class without_configuration : given.a_fallback_middleware
{
    bool _nextCalled;
    bool _result;
    Task<System.Net.HttpListenerContext>? _contextTask;
    HttpClient? _client;

    void Establish()
    {
        // No configuration
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
            ctx.Response.StatusCode = 200;
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
    }
}
