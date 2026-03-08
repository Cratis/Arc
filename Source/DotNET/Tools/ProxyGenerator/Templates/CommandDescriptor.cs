// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.ProxyGenerator.Templates;

/// <summary>
/// Describes a command for templating purposes.
/// </summary>
/// <param name="Type">The controller type that owns the command.</param>
/// <param name="Method">The method that represents the command.</param>
/// <param name="Route">API route for the command.</param>
/// <param name="Name">Name of the command.</param>
/// <param name="Properties">Properties on the command.</param>
/// <param name="Imports">Additional import statements.</param>
/// <param name="Parameters">Parameters for the request - typically in the route or query string.</param>
/// <param name="HasResponse">Whether or not there is a response from the command.</param>
/// <param name="ResponseType">The details about the response type.</param>
/// <param name="TypesInvolved">Collection of types involved in the command.</param>
/// <param name="Documentation">JSDoc documentation for the command.</param>
/// <param name="ValidationRules">Validation rules for the command properties.</param>
/// <param name="TreatWarningsAsErrors">Whether warnings should be treated as errors for this command.</param>
public record CommandDescriptor(
    Type Type,
    MethodInfo Method,
    string Route,
    string Name,
    IEnumerable<PropertyDescriptor> Properties,
    IOrderedEnumerable<ImportStatement> Imports,
    IEnumerable<RequestParameterDescriptor> Parameters,
    bool HasResponse,
    ModelDescriptor ResponseType,
    IEnumerable<Type> TypesInvolved,
    string? Documentation,
    IEnumerable<PropertyValidationDescriptor> ValidationRules,
    bool TreatWarningsAsErrors) : IDescriptor;
