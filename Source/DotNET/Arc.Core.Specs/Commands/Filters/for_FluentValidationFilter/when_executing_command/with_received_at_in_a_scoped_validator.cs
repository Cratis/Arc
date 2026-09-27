// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Arc.Commands.Filters.for_FluentValidationFilter.when_executing_command;

public class with_received_at_in_a_scoped_validator : Specification
{
    readonly DateTimeOffset _received = new(2026, 8, 9, 10, 11, 12, TimeSpan.Zero);
    ReceiptObservation _observation = null!;
    CommandContext _context = null!;
    CommandResult _result = null!;

    async Task Because()
    {
        var clock = Substitute.For<TimeProvider>();
        clock.GetUtcNow().Returns(_received);
        _observation = new ReceiptObservation();
        await using var root = new ServiceCollection()
            .AddSingleton<TimeProvider>(clock)
            .AddSingleton<IOperationContextAccessor, OperationContextAccessor>()
            .AddSingleton(_observation)
            .AddTransient<CommandWithReceiptValidator>()
            .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await using var scope = root.CreateAsyncScope();
        using var receipt = OperationContextScope.Begin(scope.ServiceProvider);
        var command = new CommandWithReceipt(_received.Year);
        _context = new CommandContext(Cratis.Execution.CorrelationId.New(), typeof(CommandWithReceipt), command, [], new(), ServiceProvider: scope.ServiceProvider)
        {
            ReceivedAt = OperationContextScope.Current!.Value
        };
        var filter = new FluentValidationFilter(new ModelGraphValidator(new DiscoverableValidators(Cratis.Types.Types.Instance), new ValidatorInvoker(NullLogger<ValidatorInvoker>.Instance)));
        _result = await filter.OnExecution(_context);
    }

    [Fact] void should_resolve_the_validator_and_read_the_same_receipt_as_the_context() => _observation.ReceivedAt.ShouldEqual(_context.ReceivedAt);
    [Fact] void should_validate_using_the_receipt() => _result.ValidationResults.ShouldBeEmpty();
}
