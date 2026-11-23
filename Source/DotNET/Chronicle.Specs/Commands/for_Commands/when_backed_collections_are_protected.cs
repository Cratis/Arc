// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Cratis.Arc.Chronicle.Testing.Commands.for_Commands;

public class when_backed_collections_are_protected : given.a_wiring_check
{
    void Establish()
    {
        Services.AddSingleton(Substitute.For<ICommandProtectedDecisionSupport>());
        Services.AddSingleton<IMongoCollection<Model>>(_ => throw new InvalidOperationException("Must not activate a refused backing binding"));
        Services.AddSingleton<IQueryable<Model>>(_ => throw new InvalidOperationException("Must not activate a backing query"));
    }
    void Because() => Problems = ChronicleCommandWiring.FindWiringProblems(Provider, [typeof(given.collection_bindings.OnCommand), typeof(given.collection_bindings.NullableCommand), typeof(given.collection_bindings.QueryableCommand)]);
    [Fact] void should_refuse_both_collection_declarations_before_di() => Problems.Count(_ => _.Category == "refused").ShouldEqual(2);
    [Fact] void should_keep_queryable_activation_explicitly_unsupported() => Problems.Single(_ => _.CommandType == typeof(given.collection_bindings.QueryableCommand)).Category.ShouldEqual("unsupported");
    [Fact] void should_not_activate_the_registered_backing_bindings() => Problems.Any(_ => _.Category == "activation").ShouldBeFalse();
}
