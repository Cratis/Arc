// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.PersonalAccessTokens.Contract;

/// <summary>A named behavioral result. Diagnostics contain no adapter exception text or credentials.</summary>
/// <param name="Name">Stable case name.</param>
/// <param name="Passed">Whether the case passed.</param>
/// <param name="Failure">Sanitized failure description.</param>
public sealed record PatContractResult(string Name, bool Passed, string? Failure);
