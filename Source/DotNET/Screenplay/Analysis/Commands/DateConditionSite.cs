// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Aggregates;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// Represents where a date condition is read.
/// </summary>
/// <param name="Model">The semantic model.</param>
/// <param name="Owner">The command.</param>
/// <param name="Location">The diagnostic location.</param>
/// <param name="Bindings">The call-site bindings.</param>
/// <param name="Diagnostics">The diagnostic collector.</param>
internal readonly record struct DateConditionSite(SemanticModel Model, ITypeSymbol Owner, string Location, ParameterBindings? Bindings, ScreenplayDiagnostics Diagnostics);
