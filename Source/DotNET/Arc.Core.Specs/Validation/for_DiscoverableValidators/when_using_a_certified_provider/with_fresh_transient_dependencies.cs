// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Validation.for_DiscoverableValidators.when_using_a_certified_provider;

public class with_fresh_transient_dependencies : given.a_certified_validator
{
    ReadingValidator _first;
    ReadingValidator _second;
    Exception _invocationError;

    void Because()
    {
        Build();
        _validators.TryGet(typeof(Model), _scope.ServiceProvider, out var first).ShouldBeTrue();
        _validators.TryGet(typeof(Model), _scope.ServiceProvider, out var second).ShouldBeTrue();
        _first = (ReadingValidator)first;
        _second = (ReadingValidator)second;
        _invocationError = Catch.Exception(() => CertifiedProtectedValidatorConstruction.VerifyInvocation(first, _scope.ServiceProvider));
    }

    [Fact] void should_construct_a_fresh_validator_per_lookup() => _first.ShouldNotBeSame(_second);
    [Fact] void should_construct_fresh_transitive_dependencies() => _first.Fresh.ShouldNotBeSame(_second.Fresh);
    [Fact] void should_resolve_the_trusted_reader_from_the_executing_scope() => _first.Fresh.Reader.ShouldBeSame(_second.Fresh.Reader);
    [Fact] void should_certify_the_constructed_rule_components() => ProtectedValidatorConstruction.IsCertified(_first, typeof(ReadingValidator)).ShouldBeTrue();
    [Fact] void should_accept_invocation_in_the_same_frame() => _invocationError.ShouldBeNull();
}
