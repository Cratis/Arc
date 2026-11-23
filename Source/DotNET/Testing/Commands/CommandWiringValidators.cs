// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.DependencyInjection;
using Cratis.Arc.Validation;
using Cratis.Types;

namespace Cratis.Arc.Testing.Commands;

/// <summary>Preflights discoverable validator constructors without running validation.</summary>
/// <param name="command">The owning command.</param>
/// <param name="services">The caller's provider.</param>
/// <param name="types">The application's type universe.</param>
/// <param name="validators">The runtime discovery metadata.</param>
/// <param name="dependencies">The dependency checker.</param>
/// <param name="problems">The collected failures.</param>
sealed class CommandWiringValidators(Type command, IServiceProvider services, ITypes types, IDiscoverableValidators validators, CommandWiringDependencies dependencies, List<CommandWiringProblem> problems)
{
    /// <summary>Checks the owning command's discoverable validator, if any.</summary>
    public void Inspect()
    {
        if (!validators.HasValidatorFor(command))
        {
            return;
        }
        var validatorType = types.FindMultiple(typeof(IDiscoverableValidator<>))
            .Single(type => type.GetInterfaces().Any(candidate => candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IDiscoverableValidator<>) && candidate.GenericTypeArguments[0] == command));
        if (CommandDecisionPolicy.IsProtected)
        {
            problems.Add(new(command, validatorType.FullName!, string.Empty, "unsupported", "Protected-validator certification requires an attested invocation; use a command scenario."));
            return;
        }
        InspectOrdinary(validatorType);
    }

    void InspectOrdinary(Type validatorType)
    {
        try
        {
            // A registered instance wins in ordinary mode, just as it does at runtime.
            if (services.GetService(validatorType) is not null)
            {
                return;
            }
            var constructor = DiscoverableValidators.PublicConstructorsOf(validatorType).OrderByDescending(_ => _.GetParameters().Length).First();
            var valid = constructor.GetParameters().Select(parameter => dependencies.Inspect(parameter, isValidator: true)).ToArray();
            if (valid.All(_ => _))
            {
                validators.TryGet(command, new WithoutParameterResolvers(services), out _);
            }
        }
        catch (Exception failure)
        {
            problems.Add(new(command, $"{validatorType.FullName}.ctor", string.Empty, "activation", "Could not construct the discoverable validator; inspect Cause for the exception type.", failure));
        }
    }

    /// <summary>Uses explicit fixtures, never executing contextual resolvers.</summary>
    /// <param name="inner">The caller's scoped provider.</param>
    sealed class WithoutParameterResolvers(IServiceProvider inner) : IServiceProvider
    {
        public object? GetService(Type serviceType) => serviceType == typeof(IInstancesOf<ICanResolveParameterDependency>) ? null : inner.GetService(serviceType);
    }
}
