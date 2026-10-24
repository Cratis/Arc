// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Testing;
using Cratis.Chronicle.Testing.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Testing.Commands;

/// <summary>
/// Checks, without executing any command, that Chronicle admits every protected decision read a command declares.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="DecisionRead{T}"/> whose read model shape Chronicle refuses (children or nested objects, joins, routing by
/// an event property or a key other than the event source id, filtered or dependency-taking reducers, ...) compiles, but
/// the command fails every time it executes. Admission is decided by the client from the discovered projection
/// definitions, reducers and the read model's schema, so it is checked here with Chronicle's own admission rules and
/// the artifacts discovered for the test run, without a running Chronicle.
/// </para>
/// <para>
/// Checks <see cref="DecisionRead{T}"/> parameters of protected commands' <c>Handle</c> and <c>Provide</c> methods,
/// including inherited methods. Commands declaring read protection On also check plain Chronicle-backed model
/// parameters and discoverable validator constructor parameters, except advisory reads. Stream model parameters
/// use stream admission. The operational runtime cap is ignored. Explicit <see cref="IDecisionReads.Get{T}(ReadModelKey, CancellationToken)"/> calls
/// inside a method body are not statically visible. Projections compiled into the test run (for example spec-local projections for a production read model) take part in discovery, and can make a
/// read model ambiguous here that is admitted in production.
/// </para>
/// </remarks>
public static class DecisionReadAdmissions
{
    static readonly MethodInfo _admit = typeof(IDecisionReads).GetMethod(nameof(IDecisionReads.Admit))!;
    static readonly MethodInfo _admitForStream = typeof(IDecisionReads).GetMethod(nameof(IDecisionReads.AdmitForStream))!;

    /// <summary>
    /// Finds the protected decision reads of the given commands that Chronicle refuses.
    /// </summary>
    /// <param name="commandTypes">The command types to check. Unprotected commands are skipped.</param>
    /// <returns>The refused decision reads; empty when every read is admitted.</returns>
    public static IReadOnlyList<RefusedDecisionRead> FindRefused(IEnumerable<Type> commandTypes)
    {
        var artifacts = Defaults.Instance.ClientArtifactsProvider;
        var readModels = ReadModelServiceCollectionExtensions.DiscoverReadModelTypes(artifacts).ToHashSet();
        var reads = commandTypes
            .SelectMany(command => DeclaredDecisionReadParameters.ReadsOf(command, readModels, TypesServiceCollectionExtensions.CurrentTypeUniverse(), protection: (Declared: true, Services: null)).Select(read => (command, read)))
            .ToArray();
        if (reads.Length == 0)
        {
            return [];
        }

        using var store = new EventStoreForTesting(serviceProvider: null, clientArtifactsProvider: artifacts);
        var decisionReads = store.GetDecisionReads();
        var admissions = new Dictionary<(Type Model, bool StreamScoped), DecisionReadAdmission>();
        var refused = new List<RefusedDecisionRead>();
        foreach (var (command, read) in reads)
        {
            if (!admissions.TryGetValue(read, out var admission))
            {
                admission = Admit(decisionReads, read.Model, read.StreamScoped);
                admissions[read] = admission;
            }

            if (!admission.IsAdmitted)
            {
                refused.Add(new(command, read.Model, admission.Reason!.Value));
            }
        }

        return refused;
    }

    /// <summary>
    /// Asserts that Chronicle admits every protected decision read of the given commands.
    /// </summary>
    /// <param name="commandTypes">The command types to check. Unprotected commands are skipped.</param>
    /// <exception cref="DecisionReadsAreRefused">One or more decision reads are refused.</exception>
    public static void ShouldAdmitDecisionReadsOf(params Type[] commandTypes)
    {
        var refused = FindRefused(commandTypes);
        if (refused.Count > 0)
        {
            throw new DecisionReadsAreRefused(refused);
        }
    }

    /// <summary>
    /// Asserts that Chronicle admits every declared protected parameter read of the commands in the given assemblies.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan for commands.</param>
    /// <exception cref="DecisionReadsAreRefused">One or more decision reads are refused.</exception>
    public static void ShouldAdmitDecisionReadsIn(params Assembly[] assemblies) =>
        ShouldAdmitDecisionReadsOf([.. assemblies.SelectMany(LoadableTypes).Where(_ => !_.IsAbstract && !_.ContainsGenericParameters &&
            (Attribute.IsDefined(_, typeof(CommandAttribute)) || _.GetCustomAttributes(true).Any(attribute => attribute is IProtectedDecisionAttribute)))]);

    static DecisionReadAdmission Admit(IDecisionReads decisionReads, Type readModel, bool streamScoped)
    {
        try
        {
            var method = streamScoped ? _admitForStream : _admit;
            return (DecisionReadAdmission)method.MakeGenericMethod(readModel).Invoke(decisionReads, [])!;
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }

    static IEnumerable<Type> LoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.OfType<Type>();
        }
    }
}
