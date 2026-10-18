// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Arc.Chronicle.ReadModels.for_ProtectedReadModelKeyTraversal;

public class when_using_generated_accessors : given.a_traversal
{
    bool _valid;
    bool _invalid;

    void Establish() => _traversal = new(Options.Create(_options), _logger) { DynamicCodeSupported = false };

    void Because()
    {
        _valid = _traversal.IsValid(new GeneratedCompositeKey("first", "second"), "wire");
        _invalid = _traversal.IsValid(new GeneratedCompositeKey("first", "*"), "wire");
    }

    [Fact] void should_accept_covered_parts_without_dynamic_code() => _valid.ShouldBeTrue();
    [Fact] void should_reject_an_invalid_part_without_dynamic_code() => _invalid.ShouldBeFalse();
    [Fact] void should_not_warn_about_reflection() => _logger.Entries.ShouldBeEmpty();
}
