// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.Queries.for_QueryableExtensions.when_ordering.through_generated_registrations;

public class by_a_static_property : Specification
{
    class item
    {
        public string Name { get; init; } = string.Empty;

        public static string Label => string.Empty;
    }

    Exception _result;

    void Establish()
    {
        QueryableElementTypes.Register<item>();
        QueryableElementTypes.RegisterSortKey<item, string>();
    }

    void Because() => _result = Catch.Exception(() => new item[] { new() }.AsQueryable().OrderBy("Label"));

    [Fact] void should_throw_target_invocation() => _result.ShouldBeOfExactType<TargetInvocationException>();
    [Fact] void should_wrap_an_argument_exception() => (_result.InnerException is ArgumentException).ShouldBeTrue();
}
