// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Commands.for_CommandDecisionPolicy;

public class when_capping_read_protection_at_runtime : Specification
{
    readonly Dictionary<(ReadProtectionMode Declared, ReadProtectionMode Maximum), ReadProtectionMode> _modes = [];
    readonly List<ReadProtectionMode> _declarations = [];

    void Because()
    {
        foreach (var (type, declared) in new[] { (typeof(OffCommand), ReadProtectionMode.Off), (typeof(MeasureCommand), ReadProtectionMode.Measure), (typeof(OnCommand), ReadProtectionMode.On) })
        {
            foreach (var maximum in Enum.GetValues<ReadProtectionMode>())
            {
                var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["ReadProtection:MaximumMode"] = maximum.ToString() }).Build();
                using var services = new ServiceCollection().AddSingleton<IConfiguration>(configuration).BuildServiceProvider();
                using var policy = CommandDecisionPolicy.Begin(type, services);
                _modes[(declared, maximum)] = CommandDecisionPolicy.ReadProtection;
                _declarations.Add(CommandDecisionPolicy.DeclaredReadProtection);
            }
        }
    }

    [Fact] void should_keep_off_under_an_off_cap() => _modes[(ReadProtectionMode.Off, ReadProtectionMode.Off)].ShouldEqual(ReadProtectionMode.Off);
    [Fact] void should_not_raise_off_to_measure() => _modes[(ReadProtectionMode.Off, ReadProtectionMode.Measure)].ShouldEqual(ReadProtectionMode.Off);
    [Fact] void should_not_raise_off_to_on() => _modes[(ReadProtectionMode.Off, ReadProtectionMode.On)].ShouldEqual(ReadProtectionMode.Off);
    [Fact] void should_lower_measure_to_off() => _modes[(ReadProtectionMode.Measure, ReadProtectionMode.Off)].ShouldEqual(ReadProtectionMode.Off);
    [Fact] void should_keep_measure_under_a_measure_cap() => _modes[(ReadProtectionMode.Measure, ReadProtectionMode.Measure)].ShouldEqual(ReadProtectionMode.Measure);
    [Fact] void should_not_raise_measure_to_on() => _modes[(ReadProtectionMode.Measure, ReadProtectionMode.On)].ShouldEqual(ReadProtectionMode.Measure);
    [Fact] void should_lower_on_to_off() => _modes[(ReadProtectionMode.On, ReadProtectionMode.Off)].ShouldEqual(ReadProtectionMode.Off);
    [Fact] void should_lower_on_to_measure() => _modes[(ReadProtectionMode.On, ReadProtectionMode.Measure)].ShouldEqual(ReadProtectionMode.Measure);
    [Fact] void should_keep_on_under_an_on_cap() => _modes[(ReadProtectionMode.On, ReadProtectionMode.On)].ShouldEqual(ReadProtectionMode.On);
    [Fact] void should_not_cache_capped_values_as_declarations() => _declarations.ShouldEqual(new[] { ReadProtectionMode.Off, ReadProtectionMode.Off, ReadProtectionMode.Off, ReadProtectionMode.Measure, ReadProtectionMode.Measure, ReadProtectionMode.Measure, ReadProtectionMode.On, ReadProtectionMode.On, ReadProtectionMode.On });

    [ReadProtection(ReadProtectionMode.Off)]
    sealed class OffCommand;

    [ReadProtection(ReadProtectionMode.Measure)]
    sealed class MeasureCommand;

    [ReadProtection(ReadProtectionMode.On)]
    sealed class OnCommand;
}
