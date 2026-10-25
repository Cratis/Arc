// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using static Cratis.Arc.Chronicle.ReadModels.for_DecisionReadAdmissionStartup.given.ProvidedDeclarations;

namespace Cratis.Arc.Chronicle.ReadModels.for_DecisionReadAdmissionStartup;

public class when_matching_provided_values : Specification
{
    Dictionary<Type, string> _injected;

    void Because() => _injected = new[]
        {
            typeof(AsyncScalar), typeof(AsyncTuple), typeof(AsyncUnion), typeof(TupleUnion), typeof(UnionAlternatives),
            typeof(NestedTuple), typeof(EnumerableValue), typeof(RepeatedValue), typeof(AssignableValue), typeof(UnrelatedValue), typeof(InheritedValue),
            typeof(ControlSignal), typeof(LongTuple), typeof(NullableTuple)
        }.ToDictionary(type => type, type => string.Join(',', InjectedHandleParameters.Of(type.GetMethod("Handle")!, type.GetProvideMethod()).Select(parameter => parameter.Name)));

    [Fact] void should_await_a_scalar() => _injected[typeof(AsyncScalar)].ShouldEqual(string.Empty);
    [Fact] void should_await_and_flatten_a_tuple() => _injected[typeof(AsyncTuple)].ShouldEqual(string.Empty);
    [Fact] void should_await_a_value_task_and_unwrap_a_union() => _injected[typeof(AsyncUnion)].ShouldEqual(string.Empty);
    [Fact] void should_unwrap_union_elements_of_a_tuple() => _injected[typeof(TupleUnion)].ShouldEqual(string.Empty);
    [Fact] void should_keep_union_alternatives_separate() => _injected[typeof(UnionAlternatives)].ShouldEqual("second");
    [Fact] void should_not_recursively_flatten_nested_tuples() => _injected[typeof(NestedTuple)].ShouldEqual("balance");
    [Fact] void should_not_flatten_enumerable_values() => _injected[typeof(EnumerableValue)].ShouldEqual("balance");
    [Fact] void should_consume_each_candidate_only_once() => _injected[typeof(RepeatedValue)].ShouldEqual("second");
    [Fact] void should_match_assignable_parameters_in_order() => _injected[typeof(AssignableValue)].ShouldEqual("balance");
    [Fact] void should_preserve_injected_parameters_not_supplied_by_provide() => _injected[typeof(UnrelatedValue)].ShouldEqual("balance");
    [Fact] void should_use_inherited_provide_methods() => _injected[typeof(InheritedValue)].ShouldEqual(string.Empty);
    [Fact] void should_drop_control_signals_before_matching() => _injected[typeof(ControlSignal)].ShouldEqual("balance");
    [Fact] void should_flatten_the_rest_slot_of_long_tuples() => _injected[typeof(LongTuple)].ShouldEqual(string.Empty);
    [Fact] void should_flatten_a_boxed_nullable_tuple() => _injected[typeof(NullableTuple)].ShouldEqual(string.Empty);
}
