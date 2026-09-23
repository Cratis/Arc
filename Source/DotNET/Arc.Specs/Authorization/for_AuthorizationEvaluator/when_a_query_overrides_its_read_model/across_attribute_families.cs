// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluator.when_a_query_overrides_its_read_model;

/// <summary>
/// A method's own declaration replaces its type's, whichever family each comes from: a read model protected with
/// Arc's attribute can open one query with ASP.NET Core's.
/// </summary>
public class across_attribute_families : given.both_attribute_families
{
    [Fact] void should_open_the_query_that_opts_out() =>
        (ArcFirst().IsAuthorized(typeof(ProtectedReadModel).GetMethod(nameof(ProtectedReadModel.Public))!) &&
         AspNetFirst().IsAuthorized(typeof(ProtectedReadModel).GetMethod(nameof(ProtectedReadModel.Public))!)).ShouldBeTrue();

    [Fact] void should_keep_the_other_query_protected() =>
        (ArcFirst().IsAuthorized(typeof(ProtectedReadModel).GetMethod(nameof(ProtectedReadModel.Protected))!) ||
         AspNetFirst().IsAuthorized(typeof(ProtectedReadModel).GetMethod(nameof(ProtectedReadModel.Protected))!)).ShouldBeFalse();
}
