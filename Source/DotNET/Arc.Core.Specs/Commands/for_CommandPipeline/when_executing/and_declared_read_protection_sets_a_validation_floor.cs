// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Commands.ResponseValueHandlers;
using Cratis.Arc.Validation;
using Cratis.Monads;
using FluentValidation;
using Microsoft.Extensions.Configuration;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_declared_read_protection_sets_a_validation_floor : given.a_command_pipeline
{
    readonly List<Recorded> _appended = [];

    void Establish()
    {
        _serviceProvider.GetService(typeof(ICommandProtectedDecisionSupport)).Returns(new DecisionSupport());
        _commandHandlerProviders.TryGetHandlerFor(Arg.Any<TestCommand>(), out Arg.Any<ICommandHandler>()).Returns(call =>
        {
            var type = call[0].GetType();
            call[1] = new ModelBoundCommandHandler(type, type.GetMethod(nameof(TestCommand.Handle))!);
            return true;
        });
        var validator = new RejectingValidator();
        _commandFilters.OnExecution(Arg.Any<CommandContext>()).Returns(async call =>
        {
            var context = call.Arg<CommandContext>();
            var validation = await validator.ValidateAsync((TestCommand)context.Command);
            return new CommandResult
            {
                CorrelationId = context.CorrelationId,
                ValidationResults = [.. validation.Errors.Select(_ => ValidationResult.Error(_.ErrorMessage))]
            };
        });
        var provide = Substitute.For<ICommandProvideInvoker>();
        provide.Invoke(Arg.Any<CommandContext>(), Arg.Any<IServiceProvider>()).Returns(call =>
            new ValueTask<IReadOnlyList<object>>(((TestCommand)call.Arg<CommandContext>().Command).Provide()));
        var resolver = new CommandHandlerArgumentResolver(provide);
        _commandHandlerArgumentResolver.Resolve(Arg.Any<ICommandHandler>(), Arg.Any<CommandContext>(), Arg.Any<IServiceProvider>(), Arg.Any<ValidationResultSeverity?>())
            .Returns(call => resolver.Resolve(call.Arg<ICommandHandler>(), call.Arg<CommandContext>(), call.Arg<IServiceProvider>(), call.Arg<ValidationResultSeverity?>()));
        _commandResponseValueHandlers.CanHandle(Arg.Any<CommandContext>(), Arg.Any<object>()).Returns(call => call[1] is Recorded or ValidationResult);
        _commandResponseValueHandlers.Handle(Arg.Any<CommandContext>(), Arg.Any<object>()).Returns(call =>
        {
            if (call[1] is Recorded recorded)
            {
                _appended.Add(recorded);
                return Task.FromResult(CommandResult.Success(_correlationId));
            }
            return new ValidationResultResponseValueHandler().Handle(call.Arg<CommandContext>(), call[1]);
        });
    }

    [Theory]
    [InlineData(ReadProtectionMode.On, false)]
    [InlineData(ReadProtectionMode.Off, true)]
    [InlineData(ReadProtectionMode.Measure, true)]
    public async Task should_only_allow_the_caller_to_skip_validator_errors_outside_on(ReadProtectionMode mode, bool succeeds)
    {
        var command = CreateCommand(mode) with { ValidatorFails = true };
        var result = await Execute(command);
        result.IsSuccess.ShouldEqual(succeeds);
        command.Handles.ShouldEqual(succeeds ? 1 : 0);
        _appended.Count.ShouldEqual(succeeds ? 1 : 0);
        result.ValidationResults.Count().ShouldEqual(succeeds ? 0 : 1);
    }

    [Theory]
    [InlineData(ReadProtectionMode.On)]
    [InlineData(ReadProtectionMode.Measure)]
    [InlineData(ReadProtectionMode.Off)]
    public async Task should_keep_the_declared_floor_when_the_runtime_cap_is_lowered(ReadProtectionMode maximum)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ReadProtection:MaximumMode"] = maximum.ToString()
        }).Build();
        _serviceProvider.GetService(typeof(IConfiguration)).Returns(configuration);
        var command = new OnCommand { ValidatorFails = true };
        var result = await Execute(command);
        result.IsSuccess.ShouldBeFalse();
        command.Handles.ShouldEqual(0);
        _appended.ShouldBeEmpty();
        result.ValidationResults.Single().Message.ShouldEqual("Validator rejected");
    }

    [Fact]
    public async Task should_block_provide_control_errors_under_on()
    {
        var command = new OnCommand { ProvideFails = true };
        var result = await Execute(command);
        result.IsSuccess.ShouldBeFalse();
        command.Handles.ShouldEqual(0);
        _appended.ShouldBeEmpty();
        result.ValidationResults.Single().Message.ShouldEqual("Provide rejected");
    }

    [Fact]
    public async Task should_not_turn_a_handle_returned_result_error_into_success_under_on()
    {
        var command = new OnCommand { HandlerFails = true };
        var result = await Execute(command);
        result.IsSuccess.ShouldBeFalse();
        command.Handles.ShouldEqual(1);
        _appended.ShouldBeEmpty();
        result.ValidationResults.Single().Message.ShouldEqual("Handle rejected");
    }

    async Task<CommandResult> Execute(TestCommand command)
    {
        var result = await _commandPipeline.Execute(command, _serviceProvider, ValidationResultSeverity.Error);
        Assert.False(result.HasExceptions, string.Join(Environment.NewLine, result.ExceptionStackTrace));
        return result;
    }

    static TestCommand CreateCommand(ReadProtectionMode mode) => mode switch
    {
        ReadProtectionMode.On => new OnCommand(),
        ReadProtectionMode.Measure => new MeasureCommand(),
        _ => new OffCommand()
    };

    public class DecisionSupport : ICommandProtectedDecisionSupport
    {
        public void EnsureSupported(IServiceProvider services)
        {
        }

        public void ValidateCommandDependency(Type dependencyType, object? dependency)
        {
        }
    }

    public record Recorded;

    public record TestCommand
    {
        public bool ValidatorFails { get; init; }
        public bool ProvideFails { get; init; }
        public bool HandlerFails { get; init; }
        public int Handles { get; private set; }

        public IReadOnlyList<object> Provide() => ProvideFails ? [ValidationResult.Error("Provide rejected")] : [];

        public Result<Recorded, ValidationResult> Handle()
        {
            Handles++;
            return HandlerFails ? ValidationResult.Error("Handle rejected") : new Recorded();
        }
    }

    [Command, ReadProtection(ReadProtectionMode.On)]
    public record OnCommand : TestCommand;

    [Command, ReadProtection(ReadProtectionMode.Off)]
    public record OffCommand : TestCommand;

    [Command, ReadProtection(ReadProtectionMode.Measure)]
    public record MeasureCommand : TestCommand;

    public class RejectingValidator : CommandValidator<TestCommand>
    {
        public RejectingValidator() => RuleFor(_ => _.ValidatorFails).Equal(false).WithMessage("Validator rejected");
    }
}
