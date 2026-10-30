// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions;

public class when_inspecting_entity_type_parameters : Specification
{
    static readonly string[] _observeMethodNames = [nameof(DbSetObserveExtensions.Observe), nameof(DbSetObserveExtensions.ObserveSingle), nameof(DbSetObserveExtensions.ObserveById)];

    List<DynamicallyAccessedMemberTypes> _methodRequirements;
    DynamicallyAccessedMemberTypes _setRequirements;

    void Because()
    {
        _methodRequirements = typeof(DbSetObserveExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(_ => _observeMethodNames.Contains(_.Name))
            .Select(_ => GetRequirements(_.GetGenericArguments()[0]))
            .ToList();
        _setRequirements = GetRequirements(typeof(QueryContextAwareSet<>).GetGenericArguments()[0]);
    }

    [Fact] void should_inspect_all_public_observe_overloads() => _methodRequirements.Count.ShouldEqual(8);
    [Fact] void should_require_entity_members_on_all_observe_overloads() => _methodRequirements.ShouldEachConformTo(_ => _ == EntityTypeMembers.Required);
    [Fact] void should_require_entity_members_on_query_context_aware_set() => _setRequirements.ShouldEqual(EntityTypeMembers.Required);

    static DynamicallyAccessedMemberTypes GetRequirements(Type parameter) =>
        parameter.GetCustomAttribute<DynamicallyAccessedMembersAttribute>()?.MemberTypes ?? DynamicallyAccessedMemberTypes.None;
}
