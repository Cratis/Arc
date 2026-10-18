// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Arc.Chronicle.ReadModels.for_ProtectedReadModelKeyTraversal;

public class when_dynamic_code_is_unavailable : given.a_traversal
{
    Exception _error;

    void Establish() => _traversal = new(Options.Create(_options), _logger) { DynamicCodeSupported = false };
    void Because() => _error = Catch.Exception(() => _traversal.IsValid(new Uncovered("secret"), "wire"));

    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<ProtectedReadModelKeyAccessorMissing>();
    [Fact] void should_name_the_missing_type() => _error.Message.ShouldContain(typeof(Uncovered).FullName!);
    [Fact] void should_explain_registration() => _error.Message.ShouldContain("ProtectedReadModelKeyAccessors.Register<T>()");
    [Fact] void should_not_disclose_the_value() => _error.Message.ShouldNotContain("secret");
    [Fact] void should_not_warn_about_an_allowed_fallback() => _logger.Entries.ShouldBeEmpty();

    record Uncovered(string Part);
}
