// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Arc.Chronicle.ReadModels.for_ProtectedReadModelKeyTraversal;

public class when_no_options_are_configured : given.a_traversal
{
    ProtectedReadModelKeyTraversal _defaultTraversal;
    Exception _error;

    void Establish() => _defaultTraversal = new(Options.Create(new ProtectedReadModelKeyOptions()), _logger) { DynamicCodeSupported = true };

    void Because() => _error = Catch.Exception(() => _defaultTraversal.IsValid(new Uncovered("valid"), "wire"));

    [Fact] void should_default_to_strict_coverage() => new ProtectedReadModelKeyOptions().Strict.ShouldBeTrue();
    [Fact] void should_refuse_an_uncovered_key_type() => _error.ShouldBeOfExactType<ProtectedReadModelKeyAccessorMissing>();

    record Uncovered(string Part);
}
