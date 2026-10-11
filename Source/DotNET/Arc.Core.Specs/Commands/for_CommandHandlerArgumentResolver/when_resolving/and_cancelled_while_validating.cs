// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using FluentValidation;

namespace Cratis.Arc.Commands.for_CommandHandlerArgumentResolver.when_resolving;

public class and_cancelled_while_validating : given.a_generated_value_resolver
{
    CancellationTokenSource _cancellation;
    Exception _error;

    void Establish()
    {
        _cancellation = new();
        _context = _context with { CancellationToken = _cancellation.Token };
        WithValidator(new CancelsValidation(_cancellation));
    }

    async Task Because() => _error = await Catch.Exception(async () => await Resolve());
    void Destroy() => _cancellation.Dispose();

    [Fact] void should_propagate_cancellation() => (_error is OperationCanceledException).ShouldBeTrue();
    [Fact] void should_not_capture_the_value() => _context.GeneratedValues.ShouldBeEmpty();

    class CancelsValidation : ConceptValidator<Id>
    {
        public CancelsValidation(CancellationTokenSource cancellation) => RuleFor(id => id.Value).MustAsync(async (_, token) =>
        {
            await cancellation.CancelAsync();
            return await Task.FromCanceled<bool>(token);
        });
    }
}
