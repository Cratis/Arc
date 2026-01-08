// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace Cratis.Arc.EntityFrameworkCore.Concepts.for_ConceptAsEvaluatableExpressionFilter.when_checking_is_evaluatable;

public class with_concept_constant_expression : given.an_evaluatable_expression_filter
{
    Expression _expression;
    bool _result;

    void Establish()
    {
        var concept = new TestIdConcept(Guid.NewGuid());
        _expression = Expression.Constant(concept);
    }

    void Because() => _result = _filter.IsEvaluatableExpression(_expression, _model);

    [Fact] void should_not_be_evaluatable() => _result.ShouldBeFalse();
}
