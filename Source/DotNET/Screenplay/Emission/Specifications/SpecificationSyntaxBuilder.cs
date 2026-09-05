// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Expressions;
using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Arc.Screenplay.Emission.Specifications;

/// <summary>
/// Builds the Screenplay <c>specification</c> blocks of a slice.
/// </summary>
/// <param name="naming">The <see cref="IScreenplayNaming"/> used for name conversion.</param>
/// <remarks>
/// Nothing a step states is ever left out over its name. Every other block decides what a line is from its first
/// word, so a property called after a directive collides with it; the values of a step are written one level deeper
/// than the step itself and the step takes every line beneath it as a value, whatever its first word says.
/// </remarks>
public class SpecificationSyntaxBuilder(IScreenplayNaming naming)
{
    readonly MappingSourceConverter _sources = new(naming);

    /// <summary>
    /// Builds the specifications of a slice.
    /// </summary>
    /// <param name="specifications">The scenarios the slice is specified by.</param>
    /// <returns>The specifications, ordered by name.</returns>
    public IEnumerable<SpecificationSyntax> Build(IEnumerable<SpecificationModel> specifications) =>
    [
        .. specifications
            .Select(Build)
            .OrderBy(_ => _.Name, StringComparer.Ordinal)
    ];

    /// <summary>
    /// Builds one specification.
    /// </summary>
    /// <param name="specification">The scenario to build for.</param>
    /// <returns>The <see cref="SpecificationSyntax"/>.</returns>
    SpecificationSyntax Build(SpecificationModel specification) =>
        new(
            naming.ToDeclarationName(specification.Name),
            [.. Events(specification.Given)],
            When(specification.When),
            [.. Events(specification.Then)],
            [.. specification.Errors.Select(_ => new SpecificationErrorSyntax(naming.ToStringLiteral(_) ?? string.Empty, SourceLocation.Start))],
            SourceLocation.Start,
            [.. ReadModels(specification.Given)],
            [.. ReadModels(specification.Then)]);

    /// <summary>
    /// Builds the command a scenario issued.
    /// </summary>
    /// <param name="command">The command, or <see langword="null"/> when the scenario issued none.</param>
    /// <returns>The <see cref="SpecificationCommandSyntax"/>, or <see langword="null"/>.</returns>
    /// <remarks>
    /// A scenario about a read model issues no command - the events are what happened - and the language holds that
    /// as a specification with no <c>when</c> rather than as one with an empty one.
    /// </remarks>
    SpecificationCommandSyntax? When(SpecificationStateModel? command) =>
        command is null
            ? null
            : new(naming.ToDeclarationName(command.Name), [.. Values(command)], SourceLocation.Start);

    /// <summary>
    /// Builds the states of a step that name an event.
    /// </summary>
    /// <param name="states">The states to build from.</param>
    /// <returns>The events.</returns>
    IEnumerable<SpecificationEventSyntax> Events(IEnumerable<SpecificationStateModel> states) =>
        states
            .Where(_ => _.Kind == SpecificationStateKind.Event)
            .Select(_ => new SpecificationEventSyntax(naming.ToDeclarationName(_.Name), [.. Values(_)], SourceLocation.Start));

    /// <summary>
    /// Builds the states of a step that name a read model.
    /// </summary>
    /// <param name="states">The states to build from.</param>
    /// <returns>The read models.</returns>
    IEnumerable<SpecificationReadModelSyntax> ReadModels(IEnumerable<SpecificationStateModel> states) =>
        states
            .Where(_ => _.Kind == SpecificationStateKind.ReadModel)
            .Select(_ => new SpecificationReadModelSyntax(naming.ToDeclarationName(_.Name), [.. Values(_)], SourceLocation.Start));

    /// <summary>
    /// Builds the values a step states.
    /// </summary>
    /// <param name="state">The state to build from.</param>
    /// <returns>The values.</returns>
    IEnumerable<PropertyMappingSyntax> Values(SpecificationStateModel state) =>
        state.Values.Select(_ => new PropertyMappingSyntax(naming.ToPropertyName(_.Property), _sources.Convert(_.Source), SourceLocation.Start));
}
