// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.DependencyInjection;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_using_certified_validator_construction;

public class with_authentication_replacing_the_scope : Specification
{
    CommandScenario<AuthenticatedCommand> _scenario;
    ServiceProvider _root;
    AsyncServiceScope _request;
    IServiceProvider _replacement;
    CommandResult _result;
    bool _replacementWasAttested;
    int _callbacks;

    async Task Establish()
    {
        _scenario = new CommandScenario<AuthenticatedCommand>();
        await _scenario.Validate(new(EventSourceId.New()));
        given.a_certified_command.RegisterDecisionState(_scenario.Services);
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "selected")], "scheme"));
        var resolution = Substitute.For<IAuthorizationPolicyResolution>();
        resolution.SelectPrincipal(Arg.Any<ClaimsPrincipal?>(), Arg.Any<IServiceProvider>(), Arg.Any<CancellationToken>()).Returns(principal);
        resolution.IsAuthorized(Arg.Any<AuthorizationPolicyContext>(), Arg.Any<IServiceProvider>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            _replacement = call.Arg<IServiceProvider>();
            _replacementWasAttested = ProtectedDecisionProviderAttestations.TryGetAttestation(_replacement, out _);
            return true;
        });
        _scenario.Services.AddSingleton<IAuthorizationPolicyRuntime>(new SelectedPrincipalRuntime(resolution));
        _scenario.Services.AddProtectedDecisionValidator<AuthenticatedValidator>(_ => _callbacks++);
        _root = (ServiceProvider)_scenario.Services.BuildProtectedDecisionServiceProvider();
        _request = _root.CreateAsyncScope();
    }

    async Task Because()
    {
        _result = await ((CommandPipeline)_root.GetRequiredService<ICommandPipeline>()).ExecuteHosted(
            new AuthenticatedCommand(EventSourceId.New()), _request.ServiceProvider, null, CancellationToken.None);
    }

    async Task Destroy()
    {
        await _request.DisposeAsync();
        await _root.DisposeAsync();
        await _scenario.DisposeAsync();
    }

    [Fact] void should_construct_the_validator_in_the_authenticated_replacement_scope() => _result.ShouldBeSuccessful();
    [Fact] void should_not_reuse_the_request_provider() => _replacement.ShouldNotBeSame(_request.ServiceProvider);
    [Fact] void should_attest_the_replacement_as_a_native_scope() => _replacementWasAttested.ShouldBeTrue();
    [Fact] void should_configure_the_fresh_validator_once() => _callbacks.ShouldEqual(1);
    [Fact] void should_append_once() => _scenario.AppendedEvents.Count.ShouldEqual(1);

    sealed class SelectedPrincipalRuntime(IAuthorizationPolicyResolution resolution) : IAuthorizationPolicyRuntime
    {
        public Task Validate(IReadOnlyList<AuthorizationRequirement> requirements, IServiceProvider services, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<IAuthorizationPolicyResolution> Resolve(IReadOnlyList<AuthorizationRequirement> requirements, IServiceProvider services, CancellationToken cancellationToken) => Task.FromResult(resolution);
        public IDisposable? BeginPrincipalScope(ClaimsPrincipal principal, IServiceProvider services) => null;
    }

    [Command]
    [Authorize(Policy = "certified-validator")]
    [ProtectedDecision]
    public record AuthenticatedCommand(EventSourceId EventSourceId)
    {
        public when_using_decision_mode.DecisionFinished Handle() => new(false);
    }

    public class AuthenticatedValidator(DecisionRead<when_using_decision_mode.DecisionState> read) : CommandValidator<AuthenticatedCommand>
    {
        public DecisionRead<when_using_decision_mode.DecisionState> Read { get; } = read;
    }
}
