// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.DependencyInjection;
using Cratis.Arc.Validation;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.Commands;

/// <summary>Coordinates command and provider metadata checks.</summary>
/// <param name="services">The caller's scope.</param>
/// <param name="options">The extension rules.</param>
sealed class CommandWiringInspection(IServiceProvider services, CommandWiringOptions options)
{
    readonly List<CommandWiringProblem> _problems = [];

    /// <summary>Checks commands and shared provider infrastructure.</summary>
    /// <param name="commands">The explicit commands.</param>
    /// <returns>The collected failures.</returns>
    public IEnumerable<CommandWiringProblem> Inspect(IEnumerable<Type> commands)
    {
        try
        {
            var types = services.GetRequiredService<ITypes>();
            var validators = new DiscoverableValidators(types, () => services);
            InspectResolvers(types);
            var commandTypes = commands.ToArray();
            if (commandTypes.Length == 0)
            {
                _problems.Add(new(null, "Discovery", string.Empty, "discovery", "Supply at least one concrete closed command type."));
            }
            foreach (var command in commandTypes)
            {
                InspectCommand(command, types, validators);
            }
        }
        catch (Exception failure)
        {
            _problems.Add(new(null, "Discovery", string.Empty, "discovery", "Register ITypes with the application's complete, valid discoverable-validator universe.", failure));
        }
        return _problems;
    }

    void InspectResolvers(ITypes types)
    {
        try
        {
            var resolvers = services.GetService<IInstancesOf<ICanResolveParameterDependency>>()?.AsEnumerable() ?? [];
            var registered = resolvers.Concat(services.GetServices<ICanResolveParameterDependency>())
                .Concat(services.GetServices<IAsyncParameterDependencyResolver>());
            var covered = options.Rules.SelectMany(_ => _.CoveredResolverTypes).ToHashSet();
            var resolverTypes = registered.Select(_ => _.GetType()).Concat(types.FindMultiple<ICanResolveParameterDependency>());
            foreach (var type in resolverTypes.Distinct().Where(type => !covered.Contains(type)))
            {
                _problems.Add(new(null, "Resolvers", string.Empty, "unsupported", $"Supply a wiring rule covering {type.FullName}; its resolver will not be executed."));
            }
        }
        catch (Exception failure)
        {
            _problems.Add(new(null, "Resolvers", string.Empty, "activation", "Could not enumerate the application's parameter resolvers.", failure));
        }
    }

    bool HasProtectedSupport(Type command)
    {
        try
        {
            services.GetRequiredService<ICommandProtectedDecisionSupport>().EnsureSupported(services);
            return true;
        }
        catch (Exception failure)
        {
            _problems.Add(new(command, "Protection", string.Empty, "unsupported", "Configure protected decision support in the supplied provider; ordinary DI cannot certify protected dependencies.", failure));
            return false;
        }
    }

    void InspectCommand(Type command, ITypes types, IDiscoverableValidators validators)
    {
        try
        {
            if (command.IsAbstract || command.ContainsGenericParameters || !command.IsCommand())
            {
                _problems.Add(new(command, "Handle", string.Empty, "discovery", "Supply a concrete closed [Command] type with one instance Handle method."));
                return;
            }
            using var policy = CommandDecisionPolicy.Begin(command, services);
            CommandDecisionPolicy.ThrowIfConflicting();
            if (CommandDecisionPolicy.IsProtected && !HasProtectedSupport(command))
            {
                return;
            }
            var parameters = new CommandWiringDependencies(command, services, options, _problems);
            foreach (var parameter in command.GetHandleMethod().GetParameters())
            {
                parameters.Inspect(parameter, isValidator: false);
            }
            new CommandWiringValidators(command, services, types, validators, parameters, _problems).Inspect();
        }
        catch (Exception failure)
        {
            _problems.Add(new(command, "Handle", string.Empty, "discovery", "Verify the command's Handle declaration and protection profile.", failure));
        }
    }
}
