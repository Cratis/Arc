// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.ProxyGenerator.Templates;

/// <summary>
/// Describes a query for templating purposes.
/// </summary>
/// <param name="Type">The controller type that owns the query.</param>
/// <param name="Method">The method that represents the query.</param>
/// <param name="Route">API route for the query.</param>
/// <param name="Name">Name of the query.</param>
/// <param name="Model">Type of model the query is for.</param>
/// <param name="Constructor">The JavaScript constructor for the model type.</param>
/// <param name="IsEnumerable">Whether or not the result is an enumerable or not.</param>
/// <param name="IsObservable">Whether or not it is an observable query or not.</param>
/// <param name="Imports">Additional import statements.</param>
/// <param name="Parameters">Parameters for the query.</param>
/// <param name="RequiredParameters">Parameters that are required for the query.</param>
/// <param name="Properties">Properties for the query.</param>
/// <param name="TypesInvolved">Collection of types involved in the query.</param>
/// <param name="Documentation">JSDoc documentation for the query.</param>
/// <param name="ValidationRules">Validation rules for the query parameters.</param>
/// <param name="TreatWarningsAsErrors">Whether warnings should be treated as errors for this query.</param>
public record QueryDescriptor(
    Type Type,
    MethodInfo Method,
    string Route,
    string Name,
    string Model,
    string Constructor,
    bool IsEnumerable,
    bool IsObservable,
    IOrderedEnumerable<ImportStatement> Imports,
    IEnumerable<RequestParameterDescriptor> Parameters,
    IEnumerable<RequestParameterDescriptor> RequiredParameters,
    IEnumerable<PropertyDescriptor> Properties,
    IEnumerable<Type> TypesInvolved,
    string? Documentation,
    IEnumerable<PropertyValidationDescriptor> ValidationRules,
    bool TreatWarningsAsErrors) : IDescriptor;
