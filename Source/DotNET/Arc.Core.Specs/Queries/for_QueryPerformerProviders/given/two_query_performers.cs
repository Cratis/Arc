// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryPerformerProviders.given;

public class two_query_performers : Specification
{
    protected IQueryPerformerProvider _firstProvider;
    protected IQueryPerformerProvider _secondProvider;
    protected IQueryPerformer _firstPerformer;
    protected IQueryPerformer _secondPerformer;
    protected QueryPerformerProviders _queryPerformerProviders;

    void Establish()
    {
        _firstProvider = Substitute.For<IQueryPerformerProvider>();
        _secondProvider = Substitute.For<IQueryPerformerProvider>();

        _firstPerformer = Substitute.For<IQueryPerformer>();
        _firstPerformer.FullyQualifiedName.Returns(new FullyQualifiedQueryName("First.Query"));
        _firstPerformer.Name.Returns(new QueryName("FirstQuery"));
        _firstPerformer.ReadModelType.Returns(typeof(string));

        _secondPerformer = Substitute.For<IQueryPerformer>();
        _secondPerformer.FullyQualifiedName.Returns(new FullyQualifiedQueryName("Second.Query"));
        _secondPerformer.Name.Returns(new QueryName("SecondQuery"));
        _secondPerformer.ReadModelType.Returns(typeof(int));

        _firstProvider.Performers.Returns([_firstPerformer]);
        _secondProvider.Performers.Returns([_secondPerformer]);
    }
}
