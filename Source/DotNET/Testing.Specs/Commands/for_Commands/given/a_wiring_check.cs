// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.DependencyInjection;
using Cratis.Arc.Testing.Commands;
using Cratis.Arc.Validation;
using Cratis.Types;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.Commands.for_Commands.given;

public class a_wiring_check : Specification
{
    protected IServiceCollection Services;
    protected ITypes Types;
    protected IReadOnlyList<CommandWiringProblem> Problems;
    ServiceProvider _provider;
    IServiceScope _scope;

    protected IServiceProvider Provider
    {
        get
        {
            _provider ??= Services.BuildServiceProvider();
            _scope ??= _provider.CreateScope();
            return _scope.ServiceProvider;
        }
    }

    void Establish()
    {
        Types = Substitute.For<ITypes>();
        Services = new ServiceCollection().AddSingleton(Types);
    }

    void Destroy()
    {
        _scope?.Dispose();
        _provider?.Dispose();
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ProtectedAttribute : Attribute, IProtectedDecisionAttribute;

    public interface IDependency;
    public class Dependency : IDependency;

    [Command]
    public class ValidCommand
    {
        public ValidCommand() => throw new InvalidOperationException("Command construction must not run");
        public void Handle(IDependency dependency) => throw new InvalidOperationException("Handle must not run");
        public static ValidCommand Provide() => throw new InvalidOperationException("Provide must not run");
    }

    [Command]
    public class NullableCommand
    {
        void Handle(IDependency? dependency) => throw new InvalidOperationException("Handle must not run");
    }

    [Command]
    public class NoHandle;

    public class OverloadedCommand
    {
        public void Handle() => throw new InvalidOperationException();
        public void Handle(IDependency dependency) => throw new InvalidOperationException();
    }

    [Command]
    public class OtherMissingCommand
    {
        public void Handle(string missing) => throw new InvalidOperationException();
    }

    [Command]
    [Protected]
    public class ProtectedCommand
    {
        public void Handle(IDependency dependency) => throw new InvalidOperationException();
    }

    [Command]
    public class NonChronicleLookup
    {
        public void Handle([LookUpBy<CallerKey>] IDependency dependency) => throw new InvalidOperationException();
    }

    public sealed class CallerKey : IReadModelKeyProvider
    {
        public ReadModelKeyLookup Resolve(ReadModelKeyContext context) => throw new InvalidOperationException("Must not execute caller key providers");
    }

    public class ProtectedValidator : CommandValidator<ProtectedCommand>
    {
        public ProtectedValidator() => throw new InvalidOperationException("Protected validator must not be constructed without attestation");
    }

    public class Validator : CommandValidator<ValidCommand>
    {
        public Validator(IDependency dependency)
        {
            Dependency = dependency;
            RuleFor(_ => _).Custom((_, _) => throw new InvalidOperationException("Validation rules must not run"));
        }

        public IDependency Dependency { get; }
    }

    [Command]
    public class LongestCommand : ValidCommand;

    [Command]
    public class ThrowingCommand : ValidCommand;

    public class LongestValidator : CommandValidator<LongestCommand>
    {
        public LongestValidator() { }
        public LongestValidator(IDependency dependency) => throw new InvalidOperationException("Longest constructor selected");
    }

    public class ThrowingValidator : CommandValidator<ThrowingCommand>
    {
        public ThrowingValidator() => throw new InvalidOperationException("Constructor failure");
    }

    public class NonDiscoverable : AbstractValidator<ValidCommand>;

    public sealed class Resolver : IAsyncParameterDependencyResolver
    {
        public int Calls { get; private set; }

        public bool TryResolve(IServiceProvider serviceProvider, ParameterInfo parameter, out object? dependency)
        {
            Calls++;
            dependency = null;
            return false;
        }

        public ValueTask<ParameterDependencyResolution> Resolve(IServiceProvider serviceProvider, ParameterInfo parameter)
        {
            Calls++;
            return ValueTask.FromResult(ParameterDependencyResolution.NotResolved);
        }
    }

    public sealed class Rule : ICommandWiringRule
    {
        public IEnumerable<Type> CoveredResolverTypes => [typeof(Resolver)];
        public CommandWiringVerdict Inspect(CommandWiringParameter parameter) => CommandWiringVerdict.NotApplicable;
    }
}
