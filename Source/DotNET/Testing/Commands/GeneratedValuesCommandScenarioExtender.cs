// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.Commands;

/// <summary>
/// Adds scenario-local generated value pins and captures, and freezes configuration at execution.
/// </summary>
/// <remarks>
/// Automatically discovered by the scenario and command pipeline. Validation alone does not freeze pins.
/// </remarks>
public sealed class GeneratedValuesCommandScenarioExtender : ICommandScenarioExtender, ICommandOperationExecutionScope
{
    /// <summary>
    /// The context key for the scenario-local fixture.
    /// </summary>
    internal const string ContextKey = "Arc.GeneratedValues";

    /// <inheritdoc/>
    public bool IsCommitParticipant => false;

    /// <inheritdoc/>
    public CommandCommitDisposition GetCommitDisposition(CommandContext context) => CommandCommitDisposition.NoCommit;

    /// <inheritdoc/>
    public void Extend(IServiceCollection services, IDictionary<string, object> context)
    {
        var fixture = new ScenarioGeneratedValueFixture();
        services.AddSingleton<IGeneratedValueFixture>(fixture);
        services.AddSingleton<TimeProvider>(fixture.TimeProvider);
        context[ContextKey] = fixture;
    }

    /// <inheritdoc/>
    public void Begin(CommandContext context) =>
        (context.ServiceProvider?.GetService<IGeneratedValueFixture>() as ScenarioGeneratedValueFixture)?.Freeze();

    /// <inheritdoc/>
    public Task Complete(CommandContext context, CommandResult result) => Task.CompletedTask;
}
