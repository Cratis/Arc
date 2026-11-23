// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Commands.ModelBound;

namespace Cratis.Arc.Testing.Commands;

/// <summary>Checks model-bound command wiring without constructing commands or executing Handle, Provide, or validation rules.</summary>
/// <remarks>
/// Supply an application-configured scoped provider. A minimal plain-spec provider registers <c>ITypes</c>
/// (the application's type universe) and the application's Handle and validator dependencies in a
/// <c>ServiceCollection</c>, then uses <c>BuildServiceProvider().CreateScope().ServiceProvider</c>.
/// No host is required. Dependency factories and validator constructors can execute. This does not check
/// authorization, runtime key values, model existence, or decision-read admission.
/// </remarks>
public static class CommandWiring
{
    /// <summary>Asserts wiring across assemblies using Arc's ordinary DI bindings.</summary>
    /// <param name="services">The caller-owned scoped provider.</param>
    /// <param name="assemblies">The command assemblies; at least one is required.</param>
    public static void ShouldBeExecutableIn(IServiceProvider services, params Assembly[] assemblies) =>
        ShouldBeExecutableIn(services, new CommandWiringOptions(), assemblies);

    /// <summary>Asserts wiring across assemblies with explicit extension rules.</summary>
    /// <param name="services">The caller-owned scoped provider.</param>
    /// <param name="options">The metadata rules.</param>
    /// <param name="assemblies">The command assemblies; at least one is required.</param>
    public static void ShouldBeExecutableIn(IServiceProvider services, CommandWiringOptions options, params Assembly[] assemblies)
    {
        var problems = new List<CommandWiringProblem>();
        var commands = new List<Type>();
        if (assemblies.Length == 0)
        {
            problems.Add(new(null, "Discovery", string.Empty, "discovery", "Supply at least one command assembly."));
        }
        foreach (var assembly in assemblies.Distinct())
        {
            try
            {
                var found = assembly.GetTypes().Where(IsCandidate).ToArray();
                commands.AddRange(found);
                if (found.Length == 0)
                {
                    problems.Add(new(null, "Discovery", string.Empty, "discovery", $"No concrete closed [Command] types found in {assembly.GetName().Name}."));
                }
            }
            catch (Exception failure)
            {
                problems.Add(new(null, "Discovery", string.Empty, "discovery", $"Could not load all types from {assembly.GetName().Name}.", failure));
            }
        }
        problems.AddRange(FindWiringProblems(services, options, commands));
        Assert(Sort(problems));
    }

    /// <summary>Asserts wiring for an explicit subset of commands.</summary>
    /// <param name="services">The caller-owned scoped provider.</param>
    /// <param name="commandTypes">The command types.</param>
    public static void ShouldBeExecutableOf(IServiceProvider services, params Type[] commandTypes) =>
        ShouldBeExecutableOf(services, new CommandWiringOptions(), commandTypes);

    /// <summary>Asserts wiring for an explicit subset with extension rules.</summary>
    /// <param name="services">The caller-owned scoped provider.</param>
    /// <param name="options">The metadata rules.</param>
    /// <param name="commandTypes">The command types.</param>
    public static void ShouldBeExecutableOf(IServiceProvider services, CommandWiringOptions options, params Type[] commandTypes) =>
        Assert(FindWiringProblems(services, options, commandTypes));

    /// <summary>Finds wiring failures using Arc's ordinary DI bindings.</summary>
    /// <param name="services">The caller-owned scoped provider.</param>
    /// <param name="commandTypes">The command types.</param>
    /// <returns>Deterministically ordered failures.</returns>
    public static IReadOnlyList<CommandWiringProblem> FindWiringProblems(IServiceProvider services, IEnumerable<Type> commandTypes) =>
        FindWiringProblems(services, new CommandWiringOptions(), commandTypes);

    /// <summary>Finds wiring failures without executing command bodies or extension resolvers.</summary>
    /// <param name="services">The caller-owned scoped provider.</param>
    /// <param name="options">The metadata rules.</param>
    /// <param name="commandTypes">The command types.</param>
    /// <returns>Deterministically ordered failures; unsupported evidence is a failure.</returns>
    public static IReadOnlyList<CommandWiringProblem> FindWiringProblems(IServiceProvider services, CommandWiringOptions options, IEnumerable<Type> commandTypes) =>
        Sort(new CommandWiringInspection(services, options).Inspect(commandTypes.Distinct()));

    static bool IsCandidate(Type type) => !type.IsAbstract && !type.ContainsGenericParameters && type.IsDefined(typeof(CommandAttribute), true);

    static CommandWiringProblem[] Sort(IEnumerable<CommandWiringProblem> problems) =>
        problems.OrderBy(_ => _.CommandType?.FullName, StringComparer.Ordinal)
            .ThenBy(_ => _.Member, StringComparer.Ordinal)
            .ThenBy(_ => _.Parameter, StringComparer.Ordinal)
            .ThenBy(_ => _.Category, StringComparer.Ordinal)
            .ThenBy(_ => _.Guidance, StringComparer.Ordinal).ToArray();

    static void Assert(IReadOnlyList<CommandWiringProblem> problems)
    {
        if (problems.Count > 0)
        {
            throw new CommandsAreNotExecutable(problems);
        }
    }
}
