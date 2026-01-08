// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace Cratis.Arc.EntityFrameworkCore.Concepts.for_ConceptAsQueryExpressionInterceptor;

public class when_query_compilation_starts : Specification
{
    ConceptAsQueryExpressionInterceptor _interceptor;
    Expression _originalExpression;
    Expression _result;

    void Establish()
    {
        _interceptor = new ConceptAsQueryExpressionInterceptor();

        // Create a simple query expression that has a concept constant
        var conceptValue = new TestIdConcept(Guid.NewGuid());
        _originalExpression = Expression.Constant(conceptValue);
    }

    void Because() => _result = _interceptor.QueryCompilationStarting(_originalExpression, null!);

    [Fact] void should_return_processed_expression() => _result.ShouldNotBeNull();
    [Fact] void should_keep_constant_expression() => _result.ShouldBeOfExactType<ConstantExpression>();
    [Fact] void should_keep_concept_type() => _result.Type.ShouldEqual(typeof(TestIdConcept));
    [Fact] void should_have_concept_value() => ((ConstantExpression)_result).Value.ShouldBeOfExactType<TestIdConcept>();
}
