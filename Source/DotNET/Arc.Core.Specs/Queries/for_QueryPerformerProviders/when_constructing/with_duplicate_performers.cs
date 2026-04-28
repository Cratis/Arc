// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryPerformerProviders.when_constructing;

public class with_duplicate_performers : given.two_query_performers
{
    IQueryPerformer _duplicatePerformer;
    Exception _exception;

    void Establish()
    {
        _duplicatePerformer = Substitute.For<IQueryPerformer>();
        _duplicatePerformer.FullyQualifiedName.Returns(new FullyQualifiedQueryName("First.Query"));
        _duplicatePerformer.Name.Returns(new QueryName("FirstQuery"));
        _duplicatePerformer.ReadModelType.Returns(typeof(string));

                // Update the first provider to return both performers (original + duplicate)
        _firstProvider.Performers.Returns([_firstPerformer, _duplicatePerformer]);

        // Make sure second provider still returns its performer
        _secondProvider.Performers.Returns([_secondPerformer]);
    }

    void Because() => _exception = Catch.Exception(() => _ = new QueryPerformerProviders(new KnownInstancesOf<IQueryPerformerProvider>([_firstProvider, _secondProvider])));

    [Fact] void should_throw_multiple_query_performers_for_same_read_model() => _exception.ShouldBeOfExactType<MultipleQueryPerformersForSameReadModel>();
}