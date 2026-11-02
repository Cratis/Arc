// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;

namespace Cratis.Arc.Validation.for_ValidatorInvoker;

public class when_a_validator_is_invoked_for_a_command : given.a_validator_invoker
{
    IEnumerable<ValidationResult> _results;

    async Task Because() => _results = await _invoker.Invoke(new Model(), new ModelValidator(), string.Empty, ValidationTargetKind.Command, CancellationToken.None);

    [Fact] void should_apply_the_command_rule() => _results.Single().Message.ShouldEqual("command");
    [Fact] void should_not_apply_the_query_rule() => _results.Any(_ => _.Message == "query").ShouldBeFalse();

    record Model;

    class ModelValidator : BaseValidator<Model>
    {
        public ModelValidator()
        {
            WhenCommand(() => RuleFor(x => x).Must(_ => false).WithMessage("command"));
            WhenQuery(() => RuleFor(x => x).Must(_ => false).WithMessage("query"));
        }
    }
}
