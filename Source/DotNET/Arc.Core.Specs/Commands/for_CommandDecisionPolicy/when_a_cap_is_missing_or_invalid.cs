// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Commands.for_CommandDecisionPolicy;

public class when_a_cap_is_missing_or_invalid : Specification
{
    readonly List<ReadProtectionMode> _modes = [];

    void Because()
    {
        foreach (var value in new[] { null, "", "invalid", "-1", "3" })
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["ReadProtection:MaximumMode"] = value }).Build();
            using var services = new ServiceCollection().AddSingleton<IConfiguration>(configuration).BuildServiceProvider();
            using var policy = CommandDecisionPolicy.Begin(typeof(OnCommand), services);
            _modes.Add(CommandDecisionPolicy.ReadProtection);
        }
        using (CommandDecisionPolicy.Begin(typeof(OnCommand)))
        {
            _modes.Add(CommandDecisionPolicy.ReadProtection);
        }
    }

    [Fact] void should_leave_compile_time_protection_unchanged() => _modes.ShouldEachConformTo(_ => _ == ReadProtectionMode.On);

    [ReadProtection(ReadProtectionMode.On)]
    sealed class OnCommand;
}
