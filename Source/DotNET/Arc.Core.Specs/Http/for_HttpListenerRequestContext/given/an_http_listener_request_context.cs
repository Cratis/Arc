// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Arc.Http.for_HttpListenerRequestContext.given;

public class an_http_listener_request_context : Specification
{
    protected HttpListener _listener;
    protected HttpListenerContext _context;
    protected HttpListenerRequestContext _requestContext;
    protected IServiceProvider _serviceProvider;
    protected int _port;
    protected HttpClient _httpClient;

    void Establish()
    {
        _port = Random.Shared.Next(50000, 60000);
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{_port}/");
        _listener.Start();

        _serviceProvider = NSubstitute.Substitute.For<IServiceProvider>();

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri($"http://localhost:{_port}/")
        };
    }

    protected async Task<HttpListenerContext> GetListenerContext()
    {
        var contextTask = _listener.GetContextAsync();
        _ = _httpClient.GetAsync("/test");
        return await contextTask;
    }

    void Destroy()
    {
        _httpClient.Dispose();
        _listener.Stop();
        _listener.Close();
    }
}
