// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Commands.for_CommandDecisionPolicy;

public class when_a_runtime_cap_meets_conflicting_declarations : Specification
{
    Exception? _conflict;
    CommandDecisionMode _decisionMode;
    ReadProtectionMode _readProtection;

    void Because()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["ReadProtection:MaximumMode"] = "Off" }).Build();
        using var services = new ServiceCollection().AddSingleton<IConfiguration>(configuration).BuildServiceProvider();
        using var policy = CommandDecisionPolicy.Begin(typeof(Conflicting), services);
        _decisionMode = CommandDecisionPolicy.Mode;
        _readProtection = CommandDecisionPolicy.ReadProtection;
        _conflict = Catch.Exception(CommandDecisionPolicy.ThrowIfConflicting);
    }

    [Fact] void should_not_hide_compile_time_declaration_conflicts() => _conflict.ShouldBeOfExactType<ConflictingReadProtection>();
    [Fact] void should_only_cap_read_protection() => _readProtection.ShouldEqual(ReadProtectionMode.Off);
    [Fact] void should_preserve_the_decision_profile() => _decisionMode.ShouldEqual(CommandDecisionMode.Unprotected);

    [UnprotectedForSpecs]
    [ReadProtection(ReadProtectionMode.On)]
    sealed class Conflicting;
}
