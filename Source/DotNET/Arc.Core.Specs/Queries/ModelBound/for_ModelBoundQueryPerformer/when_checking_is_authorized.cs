// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryPerformer;

public class when_checking_is_authorized : given.a_model_bound_query_performer
{
    bool _result;
    MethodInfo _method;

    void Establish()
    {
        _method = typeof(TestQuery).GetMethod(nameof(TestQuery.SimpleQuery))!;
        EstablishPerformer<TestQuery>(nameof(TestQuery.SimpleQuery));
        _authorizationEvaluator.IsAuthorized(_method).Returns(true);
    }

    void Because() => _result = _performer.IsAuthorized(_context);

    [Fact] void should_delegate_to_authorization_evaluator() => _authorizationEvaluator.Received(1).IsAuthorized(_method);
    [Fact] void should_return_result_from_authorization_evaluator() => _result.ShouldBeTrue();

#pragma warning disable CA1052 // Static holder types should be Static or NotInheritable
#pragma warning disable RCS1102 // Make class static
    public class TestQuery
#pragma warning restore RCS1102 // Make class static
#pragma warning restore CA1052 // Static holder types should be Static or NotInheritable
    {
        public static string SimpleQuery() => "result";
    }
}