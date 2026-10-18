// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels.for_ProtectedReadModelKeyTraversal;

public class when_registering_a_type_twice : given.a_traversal
{
    static when_registering_a_type_twice() => ProtectedReadModelKeyAccessors.Register<DuplicateKey>();
    Exception _error;
    void Because() => _error = Catch.Exception(() => ProtectedReadModelKeyAccessors.Register<DuplicateKey>());
    [Fact] void should_fail_deliberately_instead_of_replacing_the_descriptor() => _error.ShouldBeOfExactType<ProtectedReadModelKeyAccessorsAlreadyRegistered>();
    sealed class DuplicateKey;
}
