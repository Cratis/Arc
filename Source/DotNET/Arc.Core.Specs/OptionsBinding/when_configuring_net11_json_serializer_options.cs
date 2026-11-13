// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET11_0_OR_GREATER
using Microsoft.Extensions.Configuration;

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// The JSON serializer options that exist from .NET 11 are bound from configuration, and the instance is not replaced.
/// </summary>
[Collection("MutatesTypeDiscovery")]
public class when_configuring_net11_json_serializer_options : Specification
{
    ArcOptions _options;
    bool _defaultInfersClosedTypePolymorphism;

    void Because()
    {
        _options = new ArcOptions();
        _defaultInfersClosedTypePolymorphism = _options.JsonSerializerOptions.InferClosedTypePolymorphism;
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Cratis:Arc:JsonSerializerOptions:InferClosedTypePolymorphism"] = "true"
        }).Build().GetSection("Cratis:Arc");
        ArcOptionsBinder.Bind(_options, configuration);
    }

    [Fact] void should_default_to_not_inferring_closed_type_polymorphism() => _defaultInfersClosedTypePolymorphism.ShouldBeFalse();
    [Fact] void should_bind_the_explicit_value() => _options.JsonSerializerOptions.InferClosedTypePolymorphism.ShouldBeTrue();
    [Fact] void should_leave_the_type_classifiers_empty() => _options.JsonSerializerOptions.TypeClassifiers.ShouldBeEmpty();
}
#endif
