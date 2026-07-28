// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents what a named authorization policy requires of the caller.
/// </summary>
/// <remarks>
/// A policy is more than a role - it can require a claim to carry a value, and it can combine requirements - so the
/// requirement is a tree rather than a single name. What a policy requires is declared where the application is
/// composed rather than on the artifact using it, which is why it is recovered separately from the authorization.
/// </remarks>
public abstract record PolicyRequirementModel;
