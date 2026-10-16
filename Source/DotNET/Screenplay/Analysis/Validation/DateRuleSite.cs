// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Validation;

/// <summary>
/// Represents where a date rule is read.
/// </summary>
/// <param name="Model">The semantic model.</param>
/// <param name="Location">The diagnostic location.</param>
/// <param name="Diagnostics">The diagnostic collector.</param>
internal readonly record struct DateRuleSite(SemanticModel Model, string Location, ScreenplayDiagnostics Diagnostics);
