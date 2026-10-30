// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Arc.Queries.for_QueryableExtensions.given;

namespace Cratis.Arc.Queries.for_QueryableExtensions.when_counting_through_the_provider;

public class and_counting : Specification
{
    public record item(int Value);

    IQueryable _queryable;
    IQueryProvider _provider;
    Expression _source;
    int _result;

    void Establish()
    {
        _source = Expression.Constant(Array.Empty<item>().AsQueryable());
        _provider = Substitute.For<IQueryProvider>();
        _provider.Execute<int>(Arg.Any<Expression>()).Returns(5);
        var queryable = Substitute.For<IQueryable<item>>();
        queryable.ElementType.Returns(typeof(item));
        queryable.Expression.Returns(_source);
        queryable.Provider.Returns(_provider);
        _queryable = queryable;
    }

    void Because() => _result = _queryable.Count();

    [Fact] void should_return_the_provider_result() => _result.ShouldEqual(5);
    [Fact] void should_execute_a_count_call() => ExecutedExpression().Method.GetGenericMethodDefinition().ShouldEqual(Queryables.CountDefinition());
    [Fact] void should_count_the_source_expression() => ExecutedExpression().Arguments[0].ShouldBeSame(_source);

    MethodCallExpression ExecutedExpression() =>
        (MethodCallExpression)_provider.ReceivedCalls().Single(_ => _.GetMethodInfo().Name == nameof(IQueryProvider.Execute)).GetArguments()[0]!;
}
