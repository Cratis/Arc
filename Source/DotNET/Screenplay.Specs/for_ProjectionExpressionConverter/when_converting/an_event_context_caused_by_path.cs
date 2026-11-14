// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Projections;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Arc.Screenplay.for_ProjectionExpressionConverter.when_converting;

public class an_event_context_caused_by_path : Specification
{
    [Fact] void should_convert_the_subject() => ProjectionExpressionConverter.TryConvert("$eventContext(causedBy.subject)", out _).ShouldBeTrue();
    [Fact] void should_convert_the_name() => ProjectionExpressionConverter.TryConvert("$eventContext(causedBy.name)", out _).ShouldBeTrue();
    [Fact] void should_convert_the_user_name() => ProjectionExpressionConverter.TryConvert("$eventContext(causedBy.userName)", out _).ShouldBeTrue();
    [Fact] void should_not_convert_an_unknown_identity_property() => ProjectionExpressionConverter.TryConvert("$eventContext(causedBy.other)", out _).ShouldBeFalse();
    [Fact] void should_not_convert_an_unknown_context_property() => ProjectionExpressionConverter.TryConvert("$eventContext(foo)", out _).ShouldBeFalse();

    [Fact]
    void should_use_an_event_context_expression()
    {
        ProjectionExpressionConverter.TryConvert("$eventContext(causedBy.subject)", out var expression);
        expression.ShouldBeOfExactType<EventContextExpressionSyntax>();
    }

    [Fact]
    void should_keep_the_event_context_path()
    {
        ProjectionExpressionConverter.TryConvert("$eventContext(causedBy.subject)", out var expression);
        ((EventContextExpressionSyntax)expression).Path.ShouldEqual("causedBy.subject");
    }
}
