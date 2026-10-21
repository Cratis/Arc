// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Emission;

/// <summary>
/// Holds one admitted source declaration and its uniquely identified streams.
/// </summary>
/// <param name="Identity">The source's model and stored identity.</param>
/// <param name="IdentifierType">The source identifier type, when known.</param>
/// <param name="Streams">The admitted source-owned streams.</param>
internal record EventSourceDeclaration(RouteIdentity Identity, TypeReferenceModel? IdentifierType, IReadOnlyList<CommandRouteModel> Streams);
