// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Testing;
using Cratis.Chronicle.Testing.Events;

namespace Cratis.Arc.Chronicle.Testing.Commands;

/// <summary>
/// Checks, without executing any command, that Chronicle admits every protected decision read a command declares.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="DecisionRead{T}"/> whose read model shape Chronicle refuses (children or nested objects, joins, routing by
/// an event property or a key other than the event source id, reducers, more than one projection, ...) compiles, but
/// the command fails every time it executes. Admission is decided by the client from the discovered projection
/// definitions, reducers and the read model's schema, so it is checked here with Chronicle's own admission rules and
/// the artifacts discovered for the test run, without a running Chronicle.
/// </para>
/// <para>
/// Only <see cref="DecisionRead{T}"/> parameters of <c>[ProtectedDecision]</c> commands are checked. Explicit
/// <see cref="IDecisionReads.Get{T}"/> calls inside a method body are not statically visible. Projections compiled into
/// the test run (for example spec-local projections for a production read model) take part in discovery, and can make a
/// read model ambiguous here that is admitted in production.
/// </para>
/// </remarks>
public static class DecisionReadAdmissions
{
    static readonly MethodInfo _admit = typeof(IDecisionReads).GetMethod(nameof(IDecisionReads.Admit))!;

    /// <summary>
    /// Finds the protected decision reads of the given commands that Chronicle refuses.
    /// </summary>
    /// <param name="commandTypes">The command types to check. Commands that are not <c>[ProtectedDecision]</c> are skipped.</param>
    /// <returns>The refused decision reads; empty when every read is admitted.</returns>
    public static IReadOnlyList<RefusedDecisionRead> FindRefused(IEnumerable<Type> commandTypes)
    {
        var reads = commandTypes
            .SelectMany(command => ProtectedDecisionReadParameters.ReadModelTypesOf(command).Select(readModel => (command, readModel)))
            .ToArray();
        if (reads.Length == 0)
        {
            return [];
        }

        var decisionReads = new EventStoreForTesting(serviceProvider: null, clientArtifactsProvider: Defaults.Instance.ClientArtifactsProvider).GetDecisionReads();
        var admissions = new Dictionary<Type, DecisionReadAdmission>();
        var refused = new List<RefusedDecisionRead>();
        foreach (var (command, readModel) in reads)
        {
            if (!admissions.TryGetValue(readModel, out var admission))
            {
                admission = Admit(decisionReads, readModel);
                admissions[readModel] = admission;
            }

            if (!admission.IsAdmitted)
            {
                refused.Add(new(command, readModel, admission.Reason!.Value));
            }
        }

        return refused;
    }

    /// <summary>
    /// Asserts that Chronicle admits every protected decision read of the given commands.
    /// </summary>
    /// <param name="commandTypes">The command types to check. Commands that are not <c>[ProtectedDecision]</c> are skipped.</param>
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
    /// Asserts that Chronicle admits every protected decision read of the <c>[ProtectedDecision]</c> commands in the given assemblies.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan for commands.</param>
    /// <exception cref="DecisionReadsAreRefused">One or more decision reads are refused.</exception>
    public static void ShouldAdmitDecisionReadsIn(params Assembly[] assemblies) =>
        ShouldAdmitDecisionReadsOf([.. assemblies.SelectMany(LoadableTypes).Where(ProtectedDecisionReadParameters.IsProtected)]);

    static DecisionReadAdmission Admit(IDecisionReads decisionReads, Type readModel)
    {
        try
        {
            return (DecisionReadAdmission)_admit.MakeGenericMethod(readModel).Invoke(decisionReads, [])!;
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
