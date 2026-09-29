// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Cratis.Arc.for_JsonSerializerOptionsConfiguration.when_configuring_arc_defaults;

/// <summary>
/// A resolver the options already carry is the application's own and has to keep winning; Arc's resolver, with its
/// metadata and the reflection-based fallback, only comes after it.
/// </summary>
public class with_a_resolver_already_configured : Specification
{
    IJsonTypeInfoResolver _applicationResolver;
    JsonSerializerOptions _options;

    void Establish()
    {
        _applicationResolver = Substitute.For<IJsonTypeInfoResolver>();
        _options = new JsonSerializerOptions { TypeInfoResolver = _applicationResolver };
    }

    void Because() => _options.ConfigureArcDefaults();

    [Fact] void should_keep_the_application_resolver_first() => _options.TypeInfoResolverChain[0].ShouldEqual(_applicationResolver);
    [Fact] void should_follow_with_arc_resolver() => _options.TypeInfoResolverChain[1].ShouldBeOfExactType<ArcDefaultsJsonTypeInfoResolver>();
    [Fact] void should_have_nothing_else_in_the_chain() => _options.TypeInfoResolverChain.Count.ShouldEqual(2);
}
