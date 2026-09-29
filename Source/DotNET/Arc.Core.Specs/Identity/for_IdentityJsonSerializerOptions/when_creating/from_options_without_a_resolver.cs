// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Cratis.Arc.Identity.for_IdentityJsonSerializerOptions.when_creating;

/// <summary>
/// With no resolver - the application cleared the one Arc composes - <see cref="JsonSerializer"/> itself falls back to
/// reflection; identity details keep resolving the same way.
/// </summary>
public class from_options_without_a_resolver : Specification
{
    ArcOptions _arcOptions;
    JsonSerializerOptions _options;
    string _detailsJson;

    void Establish()
    {
        _arcOptions = new();
        _arcOptions.JsonSerializerOptions.TypeInfoResolver = null;
    }

    void Because()
    {
        _options = IdentityJsonSerializerOptions.CreateFrom(_arcOptions.JsonSerializerOptions);
        _detailsJson = JsonSerializer.Serialize(new KnownDetails("R&D"), (JsonTypeInfo<KnownDetails>)_options.GetTypeInfo(typeof(KnownDetails)));
    }

    [Fact] void should_fall_back_to_reflection_for_the_details() => _options.TypeInfoResolverChain[^1].ShouldEqual(JsonSerializerOptions.Default.TypeInfoResolver);
    [Fact] void should_serialize_the_details_with_the_arc_conventions_and_relaxed_encoding() => _detailsJson.ShouldEqual("{\"department\":\"R&D\"}");
    [Fact] void should_not_change_the_arc_options() => _arcOptions.JsonSerializerOptions.TypeInfoResolver.ShouldBeNull();

    public record KnownDetails(string Department);
}
