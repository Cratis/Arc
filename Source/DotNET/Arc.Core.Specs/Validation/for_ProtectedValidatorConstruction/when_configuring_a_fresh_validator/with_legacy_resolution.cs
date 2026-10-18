// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.DependencyInjection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Arc.Validation.for_ProtectedValidatorConstruction.when_configuring_a_fresh_validator;

public class with_legacy_resolution : for_DiscoverableValidators.when_using_a_certified_provider.given.a_certified_validator
{
    [Fact]
    public async Task should_run_the_same_configured_factory_and_preserve_its_denial_outside_protected_commands()
    {
        var calls = 0;
        _services.AddProtectedDecisionValidator<ReadingValidator>(validator =>
        {
            calls++;
            validator.RuleFor(model => model.Name).Must(_ => false).WithMessage("ownership denied");
        });
        await using var root = _services.BuildServiceProvider();
        await using var scope = root.CreateAsyncScope();
        var validator = scope.ServiceProvider.GetRequiredService<ReadingValidator>();
        var invoker = new ValidatorInvoker(NullLogger<ValidatorInvoker>.Instance);
        var result = (await invoker.Invoke(new Model(), validator, "")).ToArray();
        calls.ShouldEqual(1);
        result.Single().Message.ShouldEqual("ownership denied");
        _events.ShouldEqual(["fresh constructed", "validator constructed"]);
    }
}
