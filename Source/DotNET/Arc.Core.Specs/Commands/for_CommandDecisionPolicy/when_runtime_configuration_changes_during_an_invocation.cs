// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Commands.for_CommandDecisionPolicy;

public class when_runtime_configuration_changes_during_an_invocation : Specification
{
    ReadProtectionMode _outer;
    ReadProtectionMode _nested;
    ReadProtectionMode _restored;
    ReadProtectionMode _next;

    void Because()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["ReadProtection:MaximumMode"] = "Measure" }).Build();
        using var services = new ServiceCollection().AddSingleton<IConfiguration>(configuration).BuildServiceProvider();
        using (CommandDecisionPolicy.Begin(typeof(OnCommand), services))
        {
            configuration["ReadProtection:MaximumMode"] = "Off";
            _outer = CommandDecisionPolicy.ReadProtection;
            using (CommandDecisionPolicy.Begin(typeof(OnCommand), services))
            {
                _nested = CommandDecisionPolicy.ReadProtection;
            }
            _restored = CommandDecisionPolicy.ReadProtection;
        }
        using (CommandDecisionPolicy.Begin(typeof(OnCommand), services))
        {
            _next = CommandDecisionPolicy.ReadProtection;
        }
    }

    [Fact] void should_keep_the_active_invocation_immutable() => _outer.ShouldEqual(ReadProtectionMode.Measure);
    [Fact] void should_cap_the_nested_invocation_independently() => _nested.ShouldEqual(ReadProtectionMode.Off);
    [Fact] void should_restore_the_enclosing_invocation() => _restored.ShouldEqual(ReadProtectionMode.Measure);
    [Fact] void should_apply_the_new_cap_to_the_next_invocation() => _next.ShouldEqual(ReadProtectionMode.Off);

    [ReadProtection(ReadProtectionMode.On)]
    sealed class OnCommand;
}
