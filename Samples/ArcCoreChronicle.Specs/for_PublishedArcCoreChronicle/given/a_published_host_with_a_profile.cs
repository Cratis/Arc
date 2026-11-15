// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text;
using System.Text.Json;

namespace Samples.ArcCoreChronicle.Specs.for_PublishedArcCoreChronicle.given;

/// <summary>
/// Starts the published host and registers one profile through it, so the specs seed the Chronicle event store
/// through the published application rather than reaching into the kernel.
/// </summary>
public class a_published_host_with_a_profile : Specification
{
    protected const string DisplayName = "Acceptance Profile";

    protected PublishedArcCoreChronicle _host;
    protected string _subject;
    protected HttpStatusCode _statusCode;
    protected string _responseText;
    protected JsonElement _body;

    protected string Diagnostics => $"{_statusCode} {_responseText}{Environment.NewLine}{_host?.Output}";

    protected static string PrincipalFor(string subject) => $$"""{"userId":"{{subject}}","claims":[]}""";

    protected async Task Post(string route, string body, string? principal = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/features/protected-profiles/{route}")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        if (principal is not null)
        {
            request.Headers.Add("Cookie", $"x-ms-client-principal={Convert.ToBase64String(Encoding.UTF8.GetBytes(principal))}");
        }

        using var response = await _host.Client.SendAsync(request);
        _statusCode = response.StatusCode;
        _responseText = await _host.ReadResponse(response);
        using var document = JsonDocument.Parse(_responseText);
        _body = document.RootElement.Clone();
    }

    /// <summary>
    /// Reads a profile the way a caller would once the projection has caught up with the registration.
    /// </summary>
    /// <param name="route">The route of the read command.</param>
    /// <param name="body">The command body.</param>
    /// <param name="principal">The optional sample principal.</param>
    /// <returns>A task that completes when the read succeeded or the projection timeout elapsed.</returns>
    protected Task ReadWhenProjected(string route, string body, string? principal = null) =>
        PostUntilSuccessful(route, body, principal, TimeSpan.FromSeconds(30));

    async Task Establish()
    {
        if (Environment.GetEnvironmentVariable("CRATIS_ARC_PUBLISHED_ARCCORECHRONICLE") is null)
        {
            return;
        }

        // Each spec owns a subject, so specs stay independent on the shared kernel and event store.
        _subject = $"subject-{Guid.NewGuid():N}";
        _host = await PublishedArcCoreChronicle.Start();

        // The Chronicle client connects in the background once the host has started, so retry until it has.
        await PostUntilSuccessful(
            "register",
            $$"""{"subject":"{{_subject}}","displayName":"{{DisplayName}}"}""",
            null,
            TimeSpan.FromSeconds(90));
    }

    async Task Destroy()
    {
        if (_host is not null)
        {
            await _host.Stop();
        }
    }

    async Task PostUntilSuccessful(string route, string body, string? principal, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (true)
        {
            try
            {
                await Post(route, body, principal);
                if (_statusCode == HttpStatusCode.OK)
                {
                    return;
                }
            }
            catch (Exception error) when (error is HttpRequestException or JsonException)
            {
                _responseText = error.Message;
            }

            if (DateTime.UtcNow >= deadline)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }
    }
}
