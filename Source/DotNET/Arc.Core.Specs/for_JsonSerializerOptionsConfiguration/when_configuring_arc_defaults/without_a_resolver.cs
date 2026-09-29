// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Arc.for_JsonSerializerOptionsConfiguration.when_configuring_arc_defaults;

/// <summary>
/// Options without a resolver resolved every type through reflection; they now resolve Arc's own types through its
/// metadata, and everything else through the same reflection-based resolver <see cref="JsonSerializer"/> fell back to.
/// </summary>
public class without_a_resolver : Specification
{
    JsonSerializerOptions _options;

    void Because() => _options = new JsonSerializerOptions().ConfigureArcDefaults();

    [Fact] void should_start_with_arc_metadata() => _options.TypeInfoResolverChain[0].ShouldEqual(ArcJsonSerializerContext.Default);
    [Fact] void should_end_with_the_reflection_based_resolver_the_serializer_falls_back_to() => _options.TypeInfoResolverChain[1].ShouldEqual(JsonSerializerOptions.Default.TypeInfoResolver);
    [Fact] void should_have_nothing_else_in_the_chain() => _options.TypeInfoResolverChain.Count.ShouldEqual(2);
    [Fact] void should_not_duplicate_resolvers_when_configured_again() => new JsonSerializerOptions().ConfigureArcDefaults().ConfigureArcDefaults().TypeInfoResolverChain.Count.ShouldEqual(2);
}
