// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_QueryRowNarrower;

public class when_a_narrowed_result_cannot_be_narrowed : Specification
{
    public class Everything : IQueryRowNarrowing
    {
        public ValueTask<LambdaExpression?> GetPredicate(Type readModelType, QueryContext context) =>
            ValueTask.FromResult<LambdaExpression?>((Expression<Func<string, bool>>)(_ => true));
    }

    Exception _exception;

    async Task Because() => _exception = await Catch.Exception(async () =>
        await QueryRowNarrower.Narrow("Fleet.Vessels", typeof(string), "a single value", QueryContext.NotSet, new ServiceCollection().AddSingleton<IQueryRowNarrowing, Everything>().BuildServiceProvider()));

    [Fact] void should_refuse_rather_than_return_unnarrowed_rows() => _exception.ShouldBeOfExactType<UnsupportedQueryRowNarrowing>();
    [Fact] void should_name_the_query() => _exception.Message.ShouldContain("Fleet.Vessels");
}
