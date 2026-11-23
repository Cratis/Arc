// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;
using Cratis.Concepts;
using OneOf;

namespace Cratis.Arc.Testing.Commands.for_Commands.given;

public static class handle_bindings
{
    [Command]
    public class CancellationCommand
    {
        public void Handle(CancellationToken cancellationToken) => throw new InvalidOperationException("Must not execute");
        public CancellationToken Provide() => throw new InvalidOperationException("Must not execute");
    }

    [Command]
    public class GeneratedCommand
    {
        public void Handle([GeneratedValue] Generated value) => throw new InvalidOperationException("Must not execute");
        public int Provide() => throw new InvalidOperationException("Must not execute");
    }

    [Command]
    public class ProvidedGeneratedCommand
    {
        public void Handle([GeneratedValue] Generated value) => throw new InvalidOperationException("Must not execute");
        public Generated Provide() => throw new InvalidOperationException("Must not execute");
    }

    public sealed record Generated(Guid Value) : ConceptAs<Guid>(Value), IGeneratable<Generated>
    {
        public static Generated New() => throw new InvalidOperationException("Must not generate values");
    }

    [Command]
    public class ContextGenerationCommand
    {
        public void Handle([GeneratedValue] ContextGenerated value) => throw new InvalidOperationException("Must not execute");
    }

    public sealed record ContextGenerated(Guid Value) : ConceptAs<Guid>(Value), IGeneratableInContext<ContextGenerated>
    {
        public static ContextGenerated New(GenerationContext context) => throw new InvalidOperationException("Must not generate values");
    }

    [Command]
    public class ProvidedCommand
    {
        public void Handle(a_wiring_check.IDependency dependency) => throw new InvalidOperationException("Must not execute");
        public Task<(a_wiring_check.IDependency, CommandResult)> Provide() => throw new InvalidOperationException("Must not execute");
    }

    [Command]
    public class UnionProvidedCommand
    {
        public void Handle(a_wiring_check.IDependency dependency) => throw new InvalidOperationException("Must not execute");
        public ValueTask<OneOf<a_wiring_check.IDependency, ValidationResult>> Provide() => throw new InvalidOperationException("Must not execute");
    }

    [Command]
    public class OpaqueProvidedCommand
    {
        public void Handle(a_wiring_check.IDependency dependency) => throw new InvalidOperationException("Must not execute");
        public IOneOf Provide() => throw new InvalidOperationException("Must not execute");
    }

    [Command]
    public class ControlCommand
    {
        public void Handle(a_wiring_check.IDependency dependency) => throw new InvalidOperationException("Must not execute");
        public ValueTask<ValidationResult> Provide() => throw new InvalidOperationException("Must not execute");
    }

    [Command]
    public class ContextCommand
    {
        public string Value { get; set; }
        public void Handle(ContextCommand command, CommandContext context, string value) => throw new InvalidOperationException("Must not execute");
    }

    [Command]
    public class ValidatorCancellationCommand
    {
        public void Handle() => throw new InvalidOperationException("Must not execute");
    }

    public class CancellationValidator(CancellationToken cancellationToken) : CommandValidator<ValidatorCancellationCommand>
    {
        public CancellationToken Token { get; } = cancellationToken;
    }

    public sealed class RejectingRule : ICommandWiringRule
    {
        public IEnumerable<Type> CoveredResolverTypes => [];
        public CommandWiringVerdict Inspect(CommandWiringParameter parameter) => new(true, "test-rule", "Runtime-supplied parameters must bypass resolver rules.");
    }
}
