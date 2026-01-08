// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace Cratis.Arc.EntityFrameworkCore.Concepts.for_ConceptAsParameterEvaluator.when_evaluating;

public class with_binary_expression_comparing_concepts : Specification
{
    Expression _expression;
    Expression _result;
    Guid _testId;

    void Establish()
    {
        _testId = Guid.NewGuid();
        var closure = new TestClosure { Id = new TestIdConcept(_testId) };

        // Create expression: closure.Id == entity.Id
        // The evaluator should unwrap closure.Id to a constant with the underlying Guid value
        var closureConstant = Expression.Constant(closure);
        var closureIdAccess = Expression.Property(closureConstant, nameof(TestClosure.Id));

        var entity = new TestEntity { Id = new TestIdConcept(_testId) };
        var entityConstant = Expression.Constant(entity);
        var entityIdAccess = Expression.Property(entityConstant, nameof(TestEntity.Id));

        _expression = Expression.Equal(closureIdAccess, entityIdAccess);
    }

    void Because() => _result = ConceptAsParameterEvaluator.Evaluate(_expression);

    [Fact] void should_return_binary_expression() => (_result is BinaryExpression).ShouldBeTrue();
    [Fact] void should_keep_left_as_constant() => ((BinaryExpression)_result).Left.ShouldBeOfExactType<ConstantExpression>();
    [Fact] void should_keep_concept_type_on_left() => ((BinaryExpression)_result).Left.Type.ShouldEqual(typeof(TestIdConcept));
    [Fact] void should_have_concept_value_on_left() => ((ConstantExpression)((BinaryExpression)_result).Left).Value.ShouldBeOfExactType<TestIdConcept>();
}
