// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_ValidationRulesExtractor;

/// <summary>
/// A message is declared as a factory precisely because its value is not known yet. The generator runs on a build
/// machine, in a different process from the browser that will show the message, at a different time and under
/// different ambient state - so any answer it gets by calling the factory is a guess about conditions it cannot
/// stand in for. A delegate is opaque, so the generator cannot tell a factory that returns a constant from one that
/// reads the culture, the clock or a tenant; the only guess-free move is not to call it.
/// </summary>
/// <remarks>
/// The eagerly messaged rule is asserted alongside deliberately: a literal genuinely is context-free, and dropping
/// it too would be a different, worse change. The two assertions together pin the fix to the factory branch.
/// </remarks>
public class when_extracting_a_rule_whose_message_is_deferred : Specification
{
    IEnumerable<PropertyValidationDescriptor> _result;

    void Because() => _result = ValidationRulesExtractor.ExtractValidationRules(
        typeof(TypeWithDeferredMessageValidator).Assembly,
        typeof(TypeWithDeferredMessage));

    [Fact] void should_still_mirror_the_rule_itself() =>
        _result.Single(_ => _.PropertyName == "deferredMessaged").Rules.Select(_ => _.RuleName).ShouldContainOnly("notEmpty");

    [Fact] void should_not_resolve_the_factory() =>
        _result.Single(_ => _.PropertyName == "deferredMessaged").Rules.Single().ErrorMessage.ShouldBeNull();

    [Fact] void should_keep_projecting_an_eagerly_declared_literal() =>
        _result.Single(_ => _.PropertyName == "eagerlyMessaged").Rules.Single().ErrorMessage.ShouldEqual(TypeWithDeferredMessageValidator.EagerMessage);
}
