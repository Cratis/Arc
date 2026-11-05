// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers;

/// <summary>A cached request resolution, including refusals.</summary>
/// <remarks>Internal until Cratis/Arc#3701 locks; then publish.</remarks>
/// <param name="Caller">The constructed caller, or null on refusal.</param>
/// <param name="Refusal">The refusal, or null on success.</param>
internal sealed record CallerResolution(VerifiedCaller? Caller, CallerRefusal? Refusal);
