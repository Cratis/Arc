// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Arc.for_JsonSerializerOptionsConfiguration.when_configuring_arc_defaults;

/// <summary>
/// Options without a resolver resolved every type through reflection; they now resolve Arc's own types through its
/// metadata, and everything else through the same reflection-based resolver <see cref="JsonSerializer"/> fell back to -
/// both behind the one resolver Arc adds to the chain.
/// </summary>
public class without_a_resolver : Specification
{
    JsonSerializerOptions _options;

    void Because() => _options = new JsonSerializerOptions().ConfigureArcDefaults();

    [Fact] void should_hold_only_arc_resolver() => _options.TypeInfoResolverChain.Single().ShouldBeOfExactType<ArcDefaultsJsonTypeInfoResolver>();
    [Fact] void should_bind_arc_resolver_to_the_options() => ((ArcDefaultsJsonTypeInfoResolver)_options.TypeInfoResolverChain[0]).Owner.ShouldEqual(_options);
    [Fact] void should_fall_back_to_the_reflection_based_resolver_the_serializer_falls_back_to() => ((ArcDefaultsJsonTypeInfoResolver)_options.TypeInfoResolverChain[0]).Fallback.ShouldEqual(JsonSerializerOptions.Default.TypeInfoResolver);
    [Fact] void should_not_duplicate_resolvers_when_configured_again() => new JsonSerializerOptions().ConfigureArcDefaults().ConfigureArcDefaults().TypeInfoResolverChain.Count.ShouldEqual(1);
}
