// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.Emission.Commands;

/// <summary>
/// What every command of one application is admitted against.
/// </summary>
/// <param name="Model">The full application, including concept rules.</param>
/// <param name="MaximumVersion">The executable model version cap, or null for the latest constructs.</param>
/// <param name="AuthoringOnlyConstructs">Whether to retain additional authoring-only constructs.</param>
internal sealed record CommandAdmissionScope(ApplicationModel Model, SemanticVersion? MaximumVersion, bool AuthoringOnlyConstructs);
