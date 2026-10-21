// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels.for_ProtectedReadModelKeyTraversal;

public class when_strict_mode_is_enabled : given.a_traversal
{
    Exception _error;
    void Establish() => _options.Strict = true;
    void Because() => _error = Catch.Exception(() => _traversal.IsValid(new Uncovered("valid"), "wire"));

    [Fact] void should_refuse_the_jit_fallback() => _error.ShouldBeOfExactType<ProtectedReadModelKeyAccessorMissing>();
    [Fact] void should_not_warn_about_an_allowed_fallback() => _logger.Entries.ShouldBeEmpty();

    record Uncovered(string Part);
}
