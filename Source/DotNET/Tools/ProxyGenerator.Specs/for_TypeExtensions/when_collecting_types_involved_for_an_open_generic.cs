// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions;

public class Boxed<T>
{
    public T Value { get; set; } = default!;
}

public class when_collecting_types_involved_for_an_open_generic : Specification
{
    readonly List<Type> _typesInvolved = [];

    void Because() => typeof(Boxed<>).CollectTypesInvolved(_typesInvolved);

    [Fact] void should_not_collect_the_open_definition() => _typesInvolved.ShouldNotContain(typeof(Boxed<>));
    [Fact] void should_not_collect_anything_nameless() => _typesInvolved.ShouldNotContain(_ => _.FullName is null);
}
