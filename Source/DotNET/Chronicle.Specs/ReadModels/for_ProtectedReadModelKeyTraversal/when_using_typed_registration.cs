// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels.for_ProtectedReadModelKeyTraversal;

public class when_using_typed_registration : given.a_traversal
{
    static when_using_typed_registration() => ProtectedReadModelKeyAccessors.Register<ManualKey>([new(nameof(ManualKey.Part), key => key.Part)]);
    bool _valid;
    bool _invalid;

    void Establish() => _options.Strict = true;
    void Because()
    {
        _valid = _traversal.IsValid(new ManualKey("valid"), "wire");
        _invalid = _traversal.IsValid(new ManualKey("secret#part"), "wire");
    }

    [Fact] void should_accept_a_registered_key() => _valid.ShouldBeTrue();
    [Fact] void should_reject_an_invalid_registered_part() => _invalid.ShouldBeFalse();
    [Fact] void should_not_use_the_fallback() => _logger.Entries.ShouldBeEmpty();

    record ManualKey(string Part);
}
