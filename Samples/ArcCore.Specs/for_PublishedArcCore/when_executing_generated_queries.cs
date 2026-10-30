// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json;

namespace Samples.ArcCore.Specs.for_PublishedArcCore;

public class when_executing_generated_queries : given.a_published_arc_core
{
    HttpResponseMessage _response;
    JsonElement _body;
    string _responseText;

    async Task Because()
    {
        _response = await _arcCore.Client.GetAsync("api/features/query-showcase/by-id?id=7");
        _responseText = await _arcCore.ReadResponse(_response);
        using var document = JsonDocument.Parse(_responseText);
        _body = document.RootElement.Clone();
    }

    [Fact] void should_respond_with_ok() => Assert.True(_response.StatusCode == HttpStatusCode.OK, $"{_responseText}{Environment.NewLine}{_arcCore.Output}");
    [Fact] void should_report_success() => _body.GetProperty("isSuccess").GetBoolean().ShouldBeTrue();
    [Fact] void should_return_the_requested_item() => _body.GetProperty("data").GetProperty("id").GetInt32().ShouldEqual(7);
    [Fact] void should_return_the_item_name() => _body.GetProperty("data").GetProperty("name").GetString().ShouldEqual("Item #7");
    [Fact] void should_serialize_the_updated_at_as_a_timestamp() => DateTimeOffset.TryParse(_body.GetProperty("data").GetProperty("updatedAt").GetString(), out _).ShouldBeTrue();
    [Fact] void should_use_camel_case_property_names() => (_body.GetProperty("data").TryGetProperty("updatedAt", out _) && !_body.GetProperty("data").TryGetProperty("UpdatedAt", out _)).ShouldBeTrue();

    void Destroy() => _response?.Dispose();
}
