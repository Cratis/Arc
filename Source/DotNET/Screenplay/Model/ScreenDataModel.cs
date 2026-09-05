// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents a read model a screen binds through one of the queries its slice declares.
/// </summary>
/// <param name="Query">The name of the query providing the data, as the slice declares it.</param>
/// <param name="Type">The type the query returns.</param>
/// <param name="By">The name of the parameter the query is keyed by, if it has one.</param>
/// <remarks>
/// A binding is the one part of a screen a compilation can vouch for. The component names the query it imports, and
/// that name either is a query the slice declares or is not - so what the document says about the binding comes from
/// the model rather than from reading a user interface. Everything the binding is then described with - the return
/// type, the parameter it is keyed by - comes from the query itself and never from the component.
/// </remarks>
public record ScreenDataModel(string Query, TypeReferenceModel Type, string? By);
