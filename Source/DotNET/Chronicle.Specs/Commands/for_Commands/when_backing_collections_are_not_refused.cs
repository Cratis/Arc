// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Cratis.Arc.Chronicle.Testing.Commands.for_Commands;

public class when_backing_collections_are_not_refused : given.a_wiring_check
{
    void Establish()
    {
        Artifacts.ModelBoundProjections.Returns([typeof(Model), typeof(given.collection_bindings.AdvisoryModel)]);
        Services.AddSingleton(Substitute.For<ICommandProtectedDecisionSupport>());
        Services.AddSingleton(Substitute.For<IMongoCollection<Model>>());
        Services.AddSingleton(Substitute.For<IMongoCollection<given.collection_bindings.AdvisoryModel>>());
        Services.AddSingleton(Substitute.For<IMongoCollection<given.collection_bindings.OtherModel>>());
    }
    void Because() => Problems = ChronicleCommandWiring.FindWiringProblems(Provider, [typeof(given.collection_bindings.OffCommand), typeof(given.collection_bindings.MeasureCommand), typeof(given.collection_bindings.ParameterAdvisoryCommand), typeof(given.collection_bindings.ModelAdvisoryCommand), typeof(given.collection_bindings.OtherCollectionCommand)]);
    [Fact] void should_allow_off_measure_advisory_and_non_chronicle_collections() => Problems.ShouldBeEmpty();
}
