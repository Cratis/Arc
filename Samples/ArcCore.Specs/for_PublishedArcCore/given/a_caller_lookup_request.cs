// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text;
using System.Text.Json;

namespace Samples.ArcCore.Specs.for_PublishedArcCore.given;

public class a_caller_lookup_request : a_published_arc_core
{
    protected const string CallerWithLogin = /*lang=json,strict*/ """{"userId":"caller-3587","claims":[{"typ":"login","val":"login-3587"}]}""";
    protected const string CallerWithoutLogin = /*lang=json,strict*/ """{"userId":"caller-3587"}""";
    protected HttpResponseMessage _response;
    protected JsonElement _body;
    string _responseText;

    protected async Task PostLookup(string route, string? principal = null)
    {
        if (_arcCore is null)
        {
            return;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/features/caller-keyed-lookups/{route}");
        request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
        if (principal is not null)
        {
            request.Headers.Add("Cookie", $"x-ms-client-principal={Convert.ToBase64String(Encoding.UTF8.GetBytes(principal))}");
        }

        _response = await _arcCore.Client.SendAsync(request);
        _responseText = await _arcCore.ReadResponse(_response);
        using var document = JsonDocument.Parse(_responseText);
        _body = document.RootElement.Clone();
    }

    protected void AssertConfigurationFailure() => Assert.True(_response.StatusCode == HttpStatusCode.InternalServerError, $"{_responseText}{Environment.NewLine}{_arcCore.Output}");

    protected Task AssertUnsupportedLookup(string provider, string command)
    {
        // The published host runs in Development (the sample cookie authentication only works there),
        // where exception details are returned to the caller instead of being redacted and logged.
        var expected = $"LookUpBy<{provider}> on command 'Samples.Features.CallerKeyedLookups.{command}' is invalid: is only supported for Chronicle read models.";
        var messages = _body.GetProperty("exceptionMessages").EnumerateArray().Select(message => message.GetString()).ToArray();
        Assert.True(messages.Contains(expected), $"{_responseText}{Environment.NewLine}{_arcCore.Output}");
        return Task.CompletedTask;
    }

    void Destroy() => _response?.Dispose();
}
