// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.ReadModels;
using Cratis.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// Reports refused declared decision reads after Chronicle's host startup has completed.
/// </summary>
/// <param name="handlers">The known command handlers.</param>
/// <param name="configuration">The startup admission policy.</param>
/// <param name="lifetime">The host lifecycle.</param>
/// <param name="startupReads">The event-store admission reader.</param>
/// <param name="types">The validator type universe.</param>
/// <param name="services">The effective read-protection configuration.</param>
/// <param name="logger">The diagnostic logger.</param>
/// <param name="artifacts">The optional read-model artifacts.</param>
internal sealed class DecisionReadAdmissionStartup(
    ICommandHandlerProviders handlers,
    IConfiguration configuration,
    IHostApplicationLifetime lifetime,
    IStartupDecisionReads startupReads,
    ITypes types,
    IServiceProvider services,
    ILogger<DecisionReadAdmissionStartup> logger,
    IClientArtifactsProvider? artifacts = null) : IHostedService
{
    /// <summary>
    /// Gets the background check for lifecycle coordination.
    /// </summary>
    internal Task CheckTask { get; private set; } = Task.CompletedTask;

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (IsConfigured("Off")) return Task.CompletedTask;
        var readModels = artifacts is null ? [] : ReadModelServiceCollectionExtensions.DiscoverReadModelTypes(artifacts).ToHashSet();
        var reads = handlers.Handlers.Select(handler => handler.CommandType).Distinct()
            .SelectMany(command => ReadsOf(command, readModels)).ToArray();
        if (reads.Length > 0)
        {
            lifetime.ApplicationStarted.Register(() => CheckTask = Task.Run(() => Check(reads)));
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    (Type Command, Type Model, bool StreamScoped, bool On)[] ReadsOf(Type command, IReadOnlySet<Type> readModels)
    {
        using var policy = CommandDecisionPolicy.Begin(command, services);
        var on = CommandDecisionPolicy.ReadProtection == ReadProtectionMode.On;
        return DeclaredDecisionReadParameters.ReadsOf(command, readModels, types, protection: (Declared: false, Services: services))
            .Select(read => (command, read.Model, read.StreamScoped, on)).ToArray();
    }

    async Task Check((Type Command, Type Model, bool StreamScoped, bool On)[] reads)
    {
        try
        {
            var reader = await startupReads.Get(lifetime.ApplicationStopping);
            if (reader is null)
            {
                logger.AdmissionSkipped();
                return;
            }

            var refusals = Assess(reader, reads.Select(read => (read.Model, read.StreamScoped)).Distinct());
            var stop = false;
            foreach (var read in reads.Where(read => refusals[(read.Model, read.StreamScoped)] is not null))
            {
                logger.ReadRefused(read.On ? LogLevel.Critical : LogLevel.Warning, read.Command.FullName!, read.Model.FullName!, refusals[(read.Model, read.StreamScoped)]!.Value);
                stop |= read.On;
            }
            if (stop && !IsConfigured("Log")) lifetime.StopApplication();
        }
        catch (OperationCanceledException) when (lifetime.ApplicationStopping.IsCancellationRequested)
        {
            // Host shutdown cancels the pending check.
        }
        catch (Exception exception)
        {
            logger.AdmissionFailed(exception);
        }
    }

    Dictionary<(Type Model, bool StreamScoped), DecisionReadRefusalReason?> Assess(IDecisionReads reader, IEnumerable<(Type Model, bool StreamScoped)> reads)
    {
        var refusals = new Dictionary<(Type Model, bool StreamScoped), DecisionReadRefusalReason?>();
        foreach (var read in reads)
        {
            try
            {
                refusals[read] = DecisionReadVerdict.For(reader, read.Model, read.StreamScoped);
            }
            catch (Exception exception)
            {
                refusals[read] = null;
                logger.AdmissionFailed(exception);
            }
        }
        return refusals;
    }

    bool IsConfigured(string mode) => string.Equals(configuration["ReadProtection:StartupAdmissionCheck"], mode, StringComparison.OrdinalIgnoreCase);
}
