// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver;

public class when_a_runtime_cap_disables_a_declared_chain : given.a_chained_lookup
{
    Exception? _failure;
    void Because()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Cratis:Arc:ReadProtection:MaximumMode"] = "Off" }).Build();
        using var services = new ServiceCollection().AddSingleton<IConfiguration>(configuration).BuildServiceProvider();
        _failure = Catch.Exception(() => DeclaredDecisionReadParameters.ValidateChains(typeof(Providing), new HashSet<Type> { typeof(Node) }, Substitute.For<ITypes>(), services));
    }
    [Fact] void should_refuse_the_declaration_at_startup() => _failure.ShouldBeOfExactType<InvalidLookUpBy>();
    [Fact] void should_not_acquire_any_models() => _reads.ShouldBeEmpty();
}
