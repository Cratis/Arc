// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.DependencyInjection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Validation.for_ProtectedValidatorConstruction.when_configuring_a_fresh_validator;

public class with_a_reused_or_changed_instance : for_DiscoverableValidators.when_using_a_certified_provider.given.a_certified_validator
{
    [Theory]
    [InlineData("rules")]
    [InlineData("components")]
    [InlineData("scope")]
    [InlineData("invocation")]
    [InlineData("frame")]
    [InlineData("disposed")]
    public async Task should_refuse_a_foreign_or_modified_instance_before_invocation(string change)
    {
        IRuleBuilderOptions<Model, string> rule = null;
        _services.AddProtectedDecisionValidator<ReadingValidator>(value => rule = value.RuleFor(model => model.Name).NotEmpty());
        Build();
        _validators.TryGet(typeof(Model), _scope.ServiceProvider, out var validator).ShouldBeTrue();
        IDisposable nested = null;
        ProtectedValidatorActivationFrame child = null;
        await using var otherScope = _root.CreateAsyncScope();
        var provider = _scope.ServiceProvider;
        if (change == "rules") ((ReadingValidator)validator).RuleFor(model => model.Name).NotEmpty();
        if (change == "components") rule.NotNull();
        if (change == "scope") provider = otherScope.ServiceProvider;
        if (change == "invocation") nested = CommandDecisionPolicy.Begin(typeof(Model));
        if (change == "frame") child = new();
        if (change == "disposed") _scope.Dispose();
        var error = Catch.Exception(() => CertifiedProtectedValidatorConstruction.VerifyInvocation(validator, provider));
        if (child is not null) await child.DisposeAsync();
        nested?.Dispose();
        error.ShouldBeOfExactType<DiscoverableValidatorRefusedInProtectedDecision>();
    }

    [Fact]
    public async Task should_refuse_an_instance_after_its_owning_frame_completed()
    {
        Build();
        _validators.TryGet(typeof(Model), _scope.ServiceProvider, out var validator).ShouldBeTrue();
        await _frame.DisposeAsync();
        Catch.Exception(() => CertifiedProtectedValidatorConstruction.VerifyInvocation(validator, _scope.ServiceProvider))
            .ShouldBeOfExactType<DiscoverableValidatorRefusedInProtectedDecision>();
    }
}
