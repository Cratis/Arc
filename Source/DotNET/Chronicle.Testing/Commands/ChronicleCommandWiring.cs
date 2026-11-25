// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Testing.Commands;

/// <summary>One-call Arc and Chronicle wiring checks using the caller's scoped provider and artifact universe.</summary>
/// <remarks>
/// Register ITypes, IClientArtifactsProvider, and the application's ordinary dependencies. No Chronicle connection
/// or host is required. Contextual validator dependencies require explicit fixtures. Stream routes, decision-token
/// provenance, protected contextual reads and protected validators are reported as unsupported, not approved.
/// </remarks>
public static class ChronicleCommandWiring
{
    /// <summary>Asserts wiring for all commands in the supplied assemblies.</summary>
    /// <param name="services">The caller-owned scoped provider.</param>
    /// <param name="assemblies">The command assemblies.</param>
    public static void ShouldBeExecutableIn(IServiceProvider services, params Assembly[] assemblies) =>
        CommandWiring.ShouldBeExecutableIn(services, Options(services), assemblies);

    /// <summary>Asserts wiring for an explicit subset of commands.</summary>
    /// <param name="services">The caller-owned scoped provider.</param>
    /// <param name="commandTypes">The command types.</param>
    public static void ShouldBeExecutableOf(IServiceProvider services, params Type[] commandTypes) =>
        CommandWiring.ShouldBeExecutableOf(services, Options(services), commandTypes);

    /// <summary>Finds Arc and Chronicle wiring problems, without executing command code or reads.</summary>
    /// <param name="services">The caller-owned scoped provider.</param>
    /// <param name="commandTypes">The command types.</param>
    /// <returns>The ordered wiring failures.</returns>
    public static IReadOnlyList<CommandWiringProblem> FindWiringProblems(IServiceProvider services, IEnumerable<Type> commandTypes)
    {
        try
        {
            return CommandWiring.FindWiringProblems(services, Options(services), commandTypes);
        }
        catch (CommandsAreNotExecutable failure)
        {
            return failure.Problems;
        }
    }

    static CommandWiringOptions Options(IServiceProvider services)
    {
        try
        {
            return new() { Rules = [new ChronicleCommandWiringRule(services.GetRequiredService<IClientArtifactsProvider>())] };
        }
        catch (Exception failure)
        {
            throw new CommandsAreNotExecutable([new(null, "Chronicle artifacts", string.Empty, "discovery", "Register the application's IClientArtifactsProvider with its Chronicle backing artifacts.", failure)]);
        }
    }
}
