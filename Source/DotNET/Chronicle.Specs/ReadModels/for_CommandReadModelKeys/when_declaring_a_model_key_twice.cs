// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels.for_CommandReadModelKeys;

public class when_declaring_a_model_key_twice : Specification
{
    record Twice(Guid Id);

    Type _keyType;
    bool _found;

    void Because()
    {
        CommandReadModelKeys.DeclareModelKey(typeof(Twice), typeof(Guid));
        CommandReadModelKeys.DeclareModelKey(typeof(Twice), null);
        _found = CommandReadModelKeys.TryGetKeyType(typeof(Twice), out _keyType);
    }

    [Fact] void should_find_the_model() => _found.ShouldBeTrue();
    [Fact] void should_keep_the_first_declaration() => _keyType.ShouldEqual(typeof(Guid));
}
