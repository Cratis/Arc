// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_TenantIdMiddleware.when_invoking;

public class without_tenant_id_header : given.a_tenant_id_middleware
{
    bool _nextCalled;
    bool _result;
    Task<System.Net.HttpListenerContext>? _contextTask;
    HttpClient? _client;

    void Establish()
    {
        _listener.Start();
        _client = new HttpClient();
    }

    async Task Because()
    {
        _contextTask = _listener.GetContextAsync();
        
        var request = new HttpRequestMessage(HttpMethod.Get, $"http://localhost:{_port}/test");
        // No tenant-id header
        
        var responseTask = _client.SendAsync(request);
        
        var context = await _contextTask;
        
        _nextCalled = false;
        _result = await _middleware.InvokeAsync(context, ctx =>
        {
            _nextCalled = true;
            ctx.Response.StatusCode = 200;
            ctx.Response.Close();
            return Task.FromResult(true);
        });

        await responseTask;
    }

    [Fact] void should_call_next_middleware() => _nextCalled.ShouldBeTrue();
    [Fact] void should_return_result_from_next() => _result.ShouldBeTrue();

    void Destroy()
    {
        _client?.Dispose();
    }
}
