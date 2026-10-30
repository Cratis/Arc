// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text;
using System.Text.Json;

namespace Samples.ArcCore.Specs.for_PublishedArcCore;

public class when_executing_generated_commands : given.a_published_arc_core
{
    HttpResponseMessage _response;
    JsonElement _body;
    string _responseText;

    async Task Because()
    {
        using var content = new StringContent("""{"author":"published-harness","text":"from a published executable"}""", Encoding.UTF8, "application/json");
        _response = await _arcCore.Client.PostAsync("api/features/live-feed", content);
        _responseText = await _arcCore.ReadResponse(_response);
        using var document = JsonDocument.Parse(_responseText);
        _body = document.RootElement.Clone();
    }

    [Fact] void should_respond_with_ok() => Assert.True(_response.StatusCode == HttpStatusCode.OK, $"HTTP {(int)_response.StatusCode} ({_response.StatusCode}){Environment.NewLine}{_responseText}{Environment.NewLine}{_arcCore.Output}");
    [Fact] void should_report_success() => _body.GetProperty("isSuccess").GetBoolean().ShouldBeTrue();
    [Fact] void should_return_the_author_in_the_response() => _body.GetProperty("response").GetProperty("author").GetString().ShouldEqual("published-harness");
    [Fact] void should_return_the_text_in_the_response() => _body.GetProperty("response").GetProperty("text").GetString().ShouldEqual("from a published executable");
    [Fact] void should_serialize_the_posted_at_as_a_timestamp() => DateTimeOffset.TryParse(_body.GetProperty("response").GetProperty("postedAt").GetString(), out _).ShouldBeTrue();
    [Fact] void should_use_camel_case_property_names() => (_body.GetProperty("response").TryGetProperty("postedAt", out _) && !_body.GetProperty("response").TryGetProperty("PostedAt", out _)).ShouldBeTrue();

    void Destroy() => _response?.Dispose();
}
