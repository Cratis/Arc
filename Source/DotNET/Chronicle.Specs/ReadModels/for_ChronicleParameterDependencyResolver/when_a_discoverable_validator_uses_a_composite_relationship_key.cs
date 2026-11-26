// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Validation;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver;

/// <summary>
/// Verifies strict traversal of a generated validator relationship key.
/// </summary>
public class when_a_discoverable_validator_uses_a_composite_relationship_key : Specification
{
    readonly CompositeKey _key = new("part", new("nested"));
    Exception? _error;
    object? _read;
    bool _valid;
    ProtectedReadModelKeyTraversal _traversal;

    void Establish() => _traversal = new(Options.Create(new ProtectedReadModelKeyOptions { Strict = true }), NullLogger<ProtectedReadModelKeyTraversal>.Instance)
    {
        DynamicCodeSupported = false
    };

    void Because() => _error = Catch.Exception(() =>
    {
        _read = ChainedReadModelKeys.PropertyFor(typeof(Source), nameof(Source.Next))!.Read(new Source(Guid.NewGuid(), _key));
        _valid = _traversal.IsValidRawKey(_read);
    });

    [Fact] void should_use_the_generated_relationship_getter() => _read.ShouldBeSame(_key);
    [Fact] void should_have_composite_coverage_without_reflection_fallback() => _error.ShouldBeNull();
    [Fact] void should_validate_nested_composite_parts_in_strict_mode() => _valid.ShouldBeTrue();

    /// <summary>
    /// Supplies a nested composite part.
    /// </summary>
    /// <param name="Part">The nested value.</param>
    public record NestedKey(string Part);

    /// <summary>
    /// Supplies a relationship-only composite key.
    /// </summary>
    /// <param name="Part">The direct value.</param>
    /// <param name="Nested">The nested value.</param>
    public record CompositeKey(string Part, NestedKey Nested);

    /// <summary>
    /// Supplies the relationship source.
    /// </summary>
    /// <param name="Id">The source identity.</param>
    /// <param name="Next">The target key.</param>
    public record Source(Guid Id, CompositeKey Next);

    /// <summary>
    /// Supplies the relationship target.
    /// </summary>
    /// <param name="Id">The composite identity.</param>
    public record Target(CompositeKey Id);

    /// <summary>
    /// Declares only a scalar root key.
    /// </summary>
    /// <param name="Root">The scalar root identity.</param>
    public record Change(Guid Root)
    {
        /// <summary>
        /// Marks the record as a command without directly referencing the composite key.
        /// </summary>
        public void Handle() { }
    }

    /// <summary>
    /// Declares the relationship only in a supported discoverable validator.
    /// </summary>
    /// <param name="target">The relationship target.</param>
    /// <param name="source">The source of the target key.</param>
    public class Validator([LookUpBy(nameof(source), nameof(Source.Next))] Target target, Source source) : AbstractValidator<Change>, IDiscoverableValidator<Change>
    {
        /// <summary>
        /// Gets the acquired target.
        /// </summary>
        public Target Target { get; } = target;

        /// <summary>
        /// Gets the source of the target key.
        /// </summary>
        public Source Source { get; } = source;
    }
}
