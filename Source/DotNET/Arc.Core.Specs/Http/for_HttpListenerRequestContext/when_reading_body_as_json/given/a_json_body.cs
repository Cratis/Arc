// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Http.for_HttpListenerRequestContext.when_reading_body_as_json.given;

public class a_json_body : Cratis.Arc.Http.for_HttpListenerRequestContext.given.an_http_listener_request_context
{
    protected string _mediaType = "application/json";
    protected string _body = "{\"value\":\"test\"}";
    protected Payload _result;
    protected Exception _exception;

    protected async Task ReadBody()
    {
        _serviceProvider.GetService(typeof(IOptions<ArcOptions>)).Returns(Options.Create(new ArcOptions()));
        var contextTask = _listener.GetContextAsync();
        _ = _httpClient.PostAsync("/test", new StringContent(_body, Encoding.UTF8, _mediaType));
        _context = await contextTask;
        _requestContext = new HttpListenerRequestContext(_context, _serviceProvider);
        try
        {
            _result = (Payload)await _requestContext.ReadBodyAsJson(typeof(Payload));
        }
        catch (Exception exception)
        {
            _exception = exception;
        }
    }

    public record Payload(string Value);
}
