// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;

namespace Cratis.Arc.Validation.for_ProtectedValidatorConstruction.when_configuring_a_fresh_validator;

public class with_cancelled_or_failed_validation : for_DiscoverableValidators.when_using_a_certified_provider.given.a_certified_validator
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_release_the_graph_when_validation_does_not_complete(bool cancelled)
    {
        Build();
        var failure = cancelled ? (Exception)new OperationCanceledException() : new InvalidOperationException("rule failed");
        var invoker = Substitute.For<IValidatorInvoker>();
        invoker.Invoke(Arg.Any<object>(), Arg.Any<IValidator>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IEnumerable<ValidationResult>>(failure));
        var graph = new ModelGraphValidator(_validators, invoker);
        var error = await Catch.Exception(() => graph.Validate(new(new Model(), _scope.ServiceProvider)));
        error.ShouldBeSame(failure);
        _events.ShouldEqual(["fresh constructed", "validator constructed", "validator disposed", "fresh disposed"]);
    }
}
