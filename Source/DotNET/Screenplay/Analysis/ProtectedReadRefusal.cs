// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Arc.Screenplay.Analysis;

/// <summary>
/// Represents a compiler refusal attributed to the read it concerns.
/// </summary>
/// <param name="Slice">The emitted name of the slice declaring the command.</param>
/// <param name="Command">The emitted name of the command.</param>
/// <param name="Alias">The emitted name of the read: its alias, or its read model when it states none.</param>
/// <param name="Diagnostic">The compiler diagnostic.</param>
internal sealed record ProtectedReadRefusal(string Slice, string Command, string Alias, Diagnostic Diagnostic);
