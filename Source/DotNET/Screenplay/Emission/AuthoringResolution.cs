// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Emission;

/// <summary>
/// What the authoring of every command in the application is resolved against.
/// </summary>
/// <param name="Systems">The operation systems that cannot be named uniquely.</param>
/// <param name="Sources">The event sources whose declarations conflict.</param>
/// <param name="ReadOwners">The namespace owning every read model with a unique readable shape and projection.</param>
/// <param name="Application">The whole application.</param>
/// <param name="Diagnostics">Where ambiguity is reported.</param>
/// <param name="AuthoringOnlyConstructs">Whether syntax-only route mappings may be retained.</param>
internal sealed record AuthoringResolution(
    HashSet<string> Systems,
    HashSet<string> Sources,
    Dictionary<string, string> ReadOwners,
    ApplicationModel Application,
    ScreenplayDiagnostics Diagnostics,
    bool AuthoringOnlyConstructs);
