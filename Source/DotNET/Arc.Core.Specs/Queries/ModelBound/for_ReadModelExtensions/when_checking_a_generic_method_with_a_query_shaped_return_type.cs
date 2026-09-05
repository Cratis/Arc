// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.Queries.ModelBound.for_ReadModelExtensions;

/// <summary>
/// The predicate every discovery path shares. Its verdict is what decides whether a method becomes a routed
/// endpoint, so a generic method has to be rejected here rather than at the invocation that would fail.
/// </summary>
public class when_checking_a_generic_method_with_a_query_shaped_return_type : Specification
{
    bool _genericIsValid;
    bool _nonGenericIsValid;

    void Because()
    {
        _genericIsValid = typeof(ReadModelWithGenericHelper)
            .GetMethod(nameof(ReadModelWithGenericHelper.CountOf), BindingFlags.NonPublic | BindingFlags.Static)!
            .IsValidQueryFor(typeof(ReadModelWithGenericHelper));

        _nonGenericIsValid = typeof(ReadModelWithGenericHelper)
            .GetMethod(nameof(ReadModelWithGenericHelper.Totals), BindingFlags.Public | BindingFlags.Static)!
            .IsValidQueryFor(typeof(ReadModelWithGenericHelper));
    }

    [Fact] void should_not_consider_the_generic_method_a_query() => _genericIsValid.ShouldBeFalse();
    [Fact] void should_still_consider_the_non_generic_method_a_query() => _nonGenericIsValid.ShouldBeTrue();
}
