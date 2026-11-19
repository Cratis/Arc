// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// Binding configuration that does not mention the JSON serializer options must leave what they serialize unchanged, on
/// every target framework, including the members only some runtimes have.
/// </summary>
[Collection("MutatesTypeDiscovery")]
public class when_serializing_after_binding_no_json_options : Specification
{
    sealed record Sample(string Name, int Count, Sample? Child);

    string _before;
    string _after;

    void Because()
    {
        var options = new ArcOptions();
        var sample = new Sample("first", 2, new Sample("second", 1, null));
        _before = JsonSerializer.Serialize(sample, options.JsonSerializerOptions);

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Cratis:Arc:Hosting:ApplicationUrl"] = "http://localhost:1234"
        }).Build().GetSection("Cratis:Arc");
        ArcOptionsBinder.Bind(options, configuration);
        _after = JsonSerializer.Serialize(sample, options.JsonSerializerOptions);
    }

    [Fact] void should_serialize_the_same_json() => _after.ShouldEqual(_before);
}
