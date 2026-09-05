// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Commands;
using Cratis.Arc.Screenplay.Analysis.Types;
using Cratis.Arc.Screenplay.Analysis.Validation;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Controllers;

/// <summary>
/// Reads the commands a controller exposes.
/// </summary>
/// <param name="types">The <see cref="TypeRegistry"/> resolving the type of each parameter.</param>
/// <param name="properties">The <see cref="PropertyReader"/> reading the input of a body shaped command.</param>
/// <param name="produces">The <see cref="ProducesReader"/> reading what each command produces.</param>
/// <param name="validators">The <see cref="ValidatorCatalog"/> holding the rules declared for each command.</param>
/// <param name="paths">The <see cref="SourcePaths"/> rewriting the path of the file each command lives in.</param>
/// <remarks>
/// A controller method taking a request body is a command whose shape is that body, and the method is its handler.
/// A method taking loose parameters instead is a command whose shape is those parameters, named after the method.
/// </remarks>
public class ControllerCommandReader(
    TypeRegistry types,
    PropertyReader properties,
    ProducesReader produces,
    ValidatorCatalog validators,
    SourcePaths paths)
{
    /// <summary>
    /// Reads a command exposed by a controller.
    /// </summary>
    /// <param name="controller">The controller declaring it.</param>
    /// <param name="method">The method exposing it.</param>
    /// <param name="location">Where the controller lives, for use in diagnostics.</param>
    /// <returns>The <see cref="CommandModel"/>.</returns>
    public CommandModel Read(INamedTypeSymbol controller, IMethodSymbol method, string location)
    {
        var body = BodyOf(method);
        var shape = body?.Type as INamedTypeSymbol;

        return new(
            shape?.Name ?? method.Name,
            Documentation.SummaryOf(method),
            shape is null ? FromParameters(method) : properties.Read(shape),
            AuthorizationReader.Read(method, controller),
            shape is null ? [] : validators.For(shape),
            produces.Read(controller, [method], location),
            shape is null ? null : ConcurrencyReader.Read(shape),
            paths.Relative(method.SourceFilePath() ?? controller.SourceFilePath()));
    }

    /// <summary>
    /// Gets the parameter carrying the request body, if the method takes one.
    /// </summary>
    /// <param name="method">The method to read.</param>
    /// <returns>The parameter, or <see langword="null"/> when the method takes loose parameters instead.</returns>
    /// <remarks>
    /// The body is the parameter that says it is, and otherwise the single parameter whose type is a model rather
    /// than a value - a request body is never a primitive.
    /// </remarks>
    static IParameterSymbol? BodyOf(IMethodSymbol method)
    {
        var declared = method.Parameters.FirstOrDefault(_ => _.HasAttribute(WellKnownTypeNames.FromBodyAttribute));
        if (declared is not null)
        {
            return declared;
        }

        var candidates = method.Parameters.Where(IsModel).ToList();

        return candidates.Count == 1 ? candidates[0] : null;
    }

    /// <summary>
    /// Determines whether a parameter carries a model rather than a value.
    /// </summary>
    /// <param name="parameter">The parameter to check.</param>
    /// <returns>True when the parameter carries a model.</returns>
    static bool IsModel(IParameterSymbol parameter) =>
        parameter.Type is INamedTypeSymbol { TypeKind: TypeKind.Class or TypeKind.Struct } named &&
        named.SpecialType == SpecialType.None &&
        named.FindBase(WellKnownTypeNames.ConceptAs) is null &&
        !ScreenplayPrimitiveNames.IsPrimitive(named) &&
        CollectionElements.ElementOf(named) is null;

    /// <summary>
    /// Reads the input of a command whose shape is the parameters of the method.
    /// </summary>
    /// <param name="method">The method to read.</param>
    /// <returns>The properties.</returns>
    IEnumerable<PropertyModel> FromParameters(IMethodSymbol method) =>
        [.. method.Parameters.Where(_ => _.Type.TypeKind != TypeKind.Interface).Select(_ => new PropertyModel(_.Name, types.Resolve(_.Type)))];
}
