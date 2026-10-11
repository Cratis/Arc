// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryMethod;

public class when_checking_fold_creators : Specification
{
    MethodInfo[] _candidates;

    void Because() => _candidates = typeof(given.FoldReadModel).GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Where(ModelBoundQueryMethod.IsCandidate).ToArray();

    [Fact] void should_exclude_a_marked_event_creator() => HasCreate(typeof(given.Placed)).ShouldBeFalse();
    [Fact] void should_exclude_an_inherited_event_creator() => HasCreate(typeof(given.Derived)).ShouldBeFalse();
    [Fact] void should_exclude_an_event_context_creator() => HasCreate(typeof(EventContext)).ShouldBeFalse();
    [Fact] void should_exclude_a_nullable_event_context_creator() => HasCreate(typeof(EventContext?)).ShouldBeFalse();
    [Fact] void should_exclude_awaitable_creators() => _candidates.Any(_ => _.ReturnType == typeof(Task<given.FoldReadModel>) || _.ReturnType == typeof(ValueTask<given.FoldReadModel>)).ShouldBeFalse();
    [Fact] void should_exclude_explicitly_opted_out_helpers() => _candidates.Any(_ => _.Name == "Helper" || _.Name == "OtherHelper").ShouldBeFalse();
    [Fact] void should_keep_a_concept_create_query() => HasCreate(typeof(given.OrderId)).ShouldBeTrue();
    [Fact] void should_keep_an_unmarked_class_create_query() => HasCreate(typeof(given.Unmarked)).ShouldBeTrue();
    [Fact] void should_keep_a_non_create_event_query() => _candidates.Any(_ => _.Name == "From").ShouldBeTrue();
    [Fact] void should_keep_a_three_parameter_create_query() => _candidates.Any(_ => _.GetParameters().Length == 3).ShouldBeTrue();
    [Fact] void should_keep_a_collection_create_query() => _candidates.Any(_ => _.ReturnType == typeof(IEnumerable<given.FoldReadModel>)).ShouldBeTrue();

    bool HasCreate(Type parameter) => _candidates.Any(_ => _.Name == "Create" && _.GetParameters().Length == 1 && _.GetParameters()[0].ParameterType == parameter);
}
