// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryPerformer.when_performing;

/// <summary>
/// When the query method itself throws, the exception is wrapped by the CLR in a
/// <see cref="System.Reflection.TargetInvocationException"/> because we invoke it via reflection.
/// The performer must unwrap it so callers see the real exception, not the reflection wrapper.
/// </summary>
public class with_exception_thrown_by_query_method : given.a_model_bound_query_performer
{
    public class QueryException : Exception
    {
        public QueryException() : base("real query error") { }
    }

    public record TestReadModel
    {
        public static TestReadModel Query(string name) => throw new QueryException();
    }

    Exception _thrownException;

    void Establish()
    {
        EstablishPerformer<TestReadModel>(nameof(TestReadModel.Query), parameters: new QueryArguments { ["name"] = "anything" });
    }

    async Task Because() => _thrownException = await Catch.Exception(PerformQuery);

    [Fact] void should_throw_the_original_exception_type() => _thrownException.ShouldBeOfExactType<QueryException>();
    [Fact] void should_not_wrap_in_target_invocation_exception() => (_thrownException is System.Reflection.TargetInvocationException).ShouldBeFalse();
    [Fact] void should_preserve_the_original_message() => _thrownException.Message.ShouldEqual("real query error");
}
