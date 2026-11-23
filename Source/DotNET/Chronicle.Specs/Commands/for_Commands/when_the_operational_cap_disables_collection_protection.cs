// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Cratis.Arc.Chronicle.Testing.Commands.for_Commands;

public class when_the_operational_cap_disables_collection_protection : given.a_wiring_check
{
    void Establish()
    {
        Services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Cratis:Arc:ReadProtection:MaximumMode"] = "Off" }).Build());
        Services.AddSingleton(Substitute.For<IMongoCollection<Model>>());
    }
    void Because() => Problems = ChronicleCommandWiring.FindWiringProblems(Provider, [typeof(given.collection_bindings.OnCommand)]);
    [Fact] void should_check_the_effective_mode_instead_of_only_the_declared_mode() => Problems.ShouldBeEmpty();
}
