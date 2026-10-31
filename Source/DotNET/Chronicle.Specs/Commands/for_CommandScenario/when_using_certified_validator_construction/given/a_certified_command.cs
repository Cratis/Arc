// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.DependencyInjection;
using Cratis.Arc.Testing.Commands;
using Cratis.Arc.Validation;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_using_certified_validator_construction.given;

public class a_certified_command : Specification
{
    protected CommandScenario<CertifiedCommand> _scenario;
    protected CertifiedCommand _command;
    protected ServiceProvider _root;
    protected IServiceScope _scope;
    protected ICommandPipeline _pipeline;

    async Task Establish()
    {
        _scenario = new CommandScenario<CertifiedCommand>();
        _command = new(EventSourceId.New());

        // Initialize the existing scenario unchanged. Its ordinary provider still refuses the new capability.
        (await _scenario.Validate(_command)).ExceptionMessages.Single().ShouldContain("Discoverable validator");
        RegisterDecisionState(_scenario.Services);
    }

    internal static void RegisterDecisionState(IServiceCollection services)
    {
        var artifacts = Substitute.For<IClientArtifactsProvider>();
        artifacts.ModelBoundProjections.Returns([typeof(when_using_decision_mode.DecisionState)]);
        services.AddReadModels(artifacts);
    }

    protected void Build()
    {
        _root = (ServiceProvider)_scenario.Services.BuildProtectedDecisionServiceProvider();
        _scope = _root.CreateScope();
        _pipeline = _root.GetRequiredService<ICommandPipeline>();
    }

    async Task Destroy()
    {
        _scope?.Dispose();
        if (_root is not null) await _root.DisposeAsync();
        await _scenario.DisposeAsync();
    }

    [Command]
    [ProtectedDecision]
    [BlockOnValidationSeverity(ValidationResultSeverity.Error)]
    public record CertifiedCommand(EventSourceId EventSourceId, EventSourceId? Nested = null)
    {
        public async Task<when_using_decision_mode.DecisionFinished> Handle(ICommandPipeline pipeline)
        {
            if (Nested is not null)
            {
                var nested = await pipeline.Execute(new CertifiedCommand(Nested));
                nested.IsSuccess.ShouldBeTrue();
            }
            return new(false);
        }
    }

    public class CertifiedValidator(DecisionRead<when_using_decision_mode.DecisionState> read) : CommandValidator<CertifiedCommand>
    {
        public DecisionRead<when_using_decision_mode.DecisionState> Read { get; } = read;
    }

    [Command]
    [ProtectedDecision]
    public record CollectedReaders(EventSourceId EventSourceId)
    {
        public when_using_decision_mode.DecisionFinished Handle() => new(false);
    }

    public class CollectedReadersValidator(IEnumerable<IDecisionReads> reads) : CommandValidator<CollectedReaders>
    {
        public IEnumerable<IDecisionReads> Reads { get; } = reads;
    }
}
