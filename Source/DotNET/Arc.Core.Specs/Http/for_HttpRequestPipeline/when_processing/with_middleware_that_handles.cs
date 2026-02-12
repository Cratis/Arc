// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_HttpRequestPipeline.when_processing;

public class with_middleware_that_handles : given.an_http_request_pipeline
{
    HttpRequestPipeline _pipeline;
    TestMiddleware _middleware;
    Task<System.Net.HttpListenerContext>? _contextTask;
    HttpClient? _client;

    void Establish()
    {
        _middleware = new TestMiddleware(true);
        _pipeline = new HttpRequestPipeline([_middleware], _logger);

        _listener.Start();
        _client = new HttpClient();
    }

    async Task Because()
    {
        _contextTask = _listener.GetContextAsync();
        
        var responseTask = _client.GetAsync($"http://localhost:{_port}/test");
        
        var context = await _contextTask;
        await _pipeline.ProcessAsync(context);
        
        await responseTask;
    }

    [Fact] void should_call_middleware() => _middleware.WasCalled.ShouldBeTrue();

    void Destroy()
    {
        _client?.Dispose();
    }

    class TestMiddleware(bool handles) : IHttpRequestMiddleware
    {
        public bool WasCalled { get; private set; }

        public Task<bool> InvokeAsync(System.Net.HttpListenerContext context, HttpRequestDelegate next)
        {
            WasCalled = true;
            if (handles)
            {
                context.Response.StatusCode = 200;
            }
            return Task.FromResult(handles);
        }
    }
}
