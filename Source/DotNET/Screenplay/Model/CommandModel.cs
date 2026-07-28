// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents the imperative intent a slice exposes.
/// </summary>
/// <param name="Name">The name of the command.</param>
/// <param name="Description">The description of the command, if it has one.</param>
/// <param name="Properties">The properties making up the input of the command.</param>
/// <param name="Authorization">What the command requires of the caller, if anything.</param>
/// <param name="Validations">The validation rules declared for the command.</param>
/// <param name="Produces">The events the command produces.</param>
/// <param name="Concurrency">The concurrency scope the command appends within, if it declares one.</param>
/// <param name="SourceFilePath">The path of the file implementing the command, if it is known.</param>
/// <remarks>
/// A command declares either <paramref name="Produces"/> or a handler file, never both - the two together do not
/// compile. The source file path is therefore only emitted as a handler when nothing is produced declaratively.
/// </remarks>
public record CommandModel(
    string Name,
    string? Description,
    IEnumerable<PropertyModel> Properties,
    AuthorizationModel? Authorization,
    IEnumerable<ValidationRuleModel> Validations,
    IEnumerable<ProducesModel> Produces,
    ConcurrencyModel? Concurrency,
    string? SourceFilePath);
