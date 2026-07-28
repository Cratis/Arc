// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents a named authorization rule declared at the document level.
/// </summary>
/// <param name="Name">The name of the policy.</param>
/// <param name="RequiresAuthentication">Whether the policy requires an authenticated caller.</param>
/// <param name="Role">The role the caller has to hold, or <see langword="null"/> when the policy names no role.</param>
public record PolicyModel(string Name, bool RequiresAuthentication, string? Role);
