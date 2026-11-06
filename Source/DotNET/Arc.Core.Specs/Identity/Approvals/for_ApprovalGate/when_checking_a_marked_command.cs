// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Identity.Approvals.for_ApprovalGate;

public class when_checking_a_marked_command : Specification
{
    bool _result;
    void Because() => _result = ApprovalGate.RequiresApproval(typeof(given.command_types.MarkedCommand));
    [Fact] void should_require_approval() => _result.ShouldBeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task should_only_validate_with_exactly_one_flow_without_beginning_it(int count)
    {
        var flow = Substitute.For<IApprovalFlow>();
        var registrations = new ServiceCollection();
        for (var index = 0; index < count; index++) registrations.AddSingleton(flow);
        await using var services = registrations.BuildServiceProvider();
        var context = CreateContext();
        var result = ApprovalGate.ForValidation(context, services);
        if (count == 1)
        {
            result.ShouldBeNull();
        }
        else
        {
            result!.IsAuthorized.ShouldBeFalse();
            result.HasExceptions.ShouldBeFalse();
            result.CorrelationId.ShouldEqual(context.CorrelationId);
            result.AuthorizationConfigurationFailed.ShouldEqual(count == 2);
        }
        flow.ReceivedCalls().ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_fail_configuration_when_a_flow_factory_throws(bool validateOnly)
    {
        var registrations = new ServiceCollection().AddSingleton<IApprovalFlow>(_ => throw new InvalidOperationException("Not returned to the caller"));
        await using var services = registrations.BuildServiceProvider();
        var context = CreateContext();
        var result = validateOnly
            ? ApprovalGate.ForValidation(context, services)
            : await ApprovalGate.ForExecution(context, services);
        result!.AuthorizationConfigurationFailed.ShouldBeTrue();
        result.IsAuthorized.ShouldBeFalse();
        result.HasExceptions.ShouldBeFalse();
    }

    [Fact]
    public async Task should_fail_configuration_when_flow_enumeration_throws()
    {
        var services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(IEnumerable<IApprovalFlow>)).Returns(FailingEnumeration());
        var result = await ApprovalGate.ForExecution(CreateContext(), services);
        result!.AuthorizationConfigurationFailed.ShouldBeTrue();
        result.IsAuthorized.ShouldBeFalse();
        result.HasExceptions.ShouldBeFalse();
    }

    [Fact]
    public async Task should_not_translate_flow_cancellation_to_configuration_failure()
    {
        var flow = Substitute.For<IApprovalFlow>();
        flow.Begin(Arg.Any<CommandContext>()).Returns(Task.FromException<CommandResult>(new OperationCanceledException()));
        await using var services = new ServiceCollection().AddSingleton(flow).BuildServiceProvider();
        await Assert.ThrowsAsync<OperationCanceledException>(() => ApprovalGate.ForExecution(CreateContext(), services));
    }

    static CommandContext CreateContext() => new(CorrelationId.New(), typeof(given.command_types.MarkedCommand), new given.command_types.MarkedCommand(), [], new CommandContextValues());

    static IEnumerable<IApprovalFlow> FailingEnumeration()
    {
        yield return Substitute.For<IApprovalFlow>();
        throw new InvalidOperationException("Not returned to the caller");
    }
}
