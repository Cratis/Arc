// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions;

public class when_collecting_types_involved_for_a_generic_parameter : Specification
{
    readonly List<Type> _typesInvolved = [];

    void Because() => typeof(Boxed<>).GetGenericArguments()[0].CollectTypesInvolved(_typesInvolved);

    [Fact] void should_collect_nothing() => _typesInvolved.ShouldBeEmpty();
}
