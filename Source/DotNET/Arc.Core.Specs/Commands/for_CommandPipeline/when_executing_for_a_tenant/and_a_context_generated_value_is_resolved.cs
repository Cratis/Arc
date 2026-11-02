// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Tenancy;
using Cratis.Arc.Validation;
using Cratis.Concepts;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing_for_a_tenant;

public class and_a_context_generated_value_is_resolved : given.a_command_pipeline_and_a_handler_for_command
{
    readonly List<TenantId> _filterTenants = [];
    CommandResult _result;
    string _generated;
    IDisposable _boundary;

    void Establish()
    {
        _boundary = TenantIdAccessor.Independent.BeginRequest();
        var resolver = Substitute.For<ITenantIdResolver>();
        resolver.Resolve().Returns("selected");
        var accessor = new TenantIdAccessor(resolver);
        var principals = Substitute.For<ICurrentPrincipalAccessor>();
        principals.Current.Returns(new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "user-1")], "test")));
        _serviceProvider.GetService(typeof(ICurrentPrincipalAccessor)).Returns(principals);
        _serviceProvider.GetService(typeof(TenantIdAccessor)).Returns(accessor);
        _serviceProvider.GetService(typeof(ITenantIdAccessor)).Returns(accessor);
        _serviceProvider.GetService(typeof(ITenantIdResolver)).Returns(resolver);
        var invoker = new ValidatorInvoker(NullLogger<ValidatorInvoker>.Instance);
        var parameter = typeof(Generating).GetMethod(nameof(Generating.Handle))!.GetParameters()[0];

        _commandFilters.OnExecution(Arg.Any<CommandContext>()).Returns(async _ =>
        {
            await invoker.Invoke("value", new RecordingValidator(_filterTenants), string.Empty);
            return CommandResult.Success(_correlationId);
        });
        _commandHandlerArgumentResolver
            .Resolve(Arg.Any<ICommandHandler>(), Arg.Any<CommandContext>(), Arg.Any<IServiceProvider>(), Arg.Any<ValidationResultSeverity?>())
            .Returns(async call =>
            {
                var context = call.Arg<CommandContext>() with { ReceivedAt = DateTimeOffset.UtcNow };
                var value = (ContextId)await GeneratedValueResolution.Resolve(typeof(Generating), parameter, context, call.Arg<IServiceProvider>());
                _generated = value.Value;
                return new CommandHandlerArgumentResolution([value], CommandResult.Success(_correlationId));
            });
    }

    async Task Because() => _result = await _commandPipeline.Execute(_command);

    void Destroy() => _boundary.Dispose();

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_generate_with_the_tenant_the_filter_read() => _generated.ShouldEqual(_filterTenants.Single().Value);

    public sealed record ContextId(string Value) : ConceptAs<string>(Value), IGeneratableInContext<ContextId>
    {
        static ContextId IGeneratableInContext<ContextId>.New(GenerationContext context) => new(context.Tenant);
    }

    public class Generating
    {
        public void Handle([GeneratedValue] ContextId id) { }
    }

    class RecordingValidator : AbstractValidator<string>
    {
        public RecordingValidator(List<TenantId> tenants) => RuleFor(_ => _).Custom((_, context) => tenants.Add(context.GetTenant()));
    }
}
