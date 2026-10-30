// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.Queries.for_QueryableExtensions.when_ordering;

public class by_an_indexer : Specification
{
    class item
    {
        public string this[int index] => string.Empty;
    }

    Exception _result;

    void Because() => _result = Catch.Exception(() => new item[] { new() }.AsQueryable().OrderBy("Item"));

    [Fact] void should_throw_target_invocation() => _result.ShouldBeOfExactType<TargetInvocationException>();
    [Fact] void should_wrap_an_argument_exception() => (_result.InnerException is ArgumentException).ShouldBeTrue();
}
