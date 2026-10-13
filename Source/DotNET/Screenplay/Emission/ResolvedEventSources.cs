// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Emission;

/// <summary>
/// Separates unique declarations from refused model names before any reference is emitted.
/// </summary>
/// <param name="Sources">The compatible source declarations.</param>
/// <param name="ConflictingSources">The source names that cannot represent all their routes.</param>
internal record ResolvedEventSources(IReadOnlyList<EventSourceDeclaration> Sources, IReadOnlySet<string> ConflictingSources);
