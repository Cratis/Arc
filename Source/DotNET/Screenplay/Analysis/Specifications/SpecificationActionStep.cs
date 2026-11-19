// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// The call a specification performs as its action.
/// </summary>
/// <param name="Step">The call.</param>
/// <param name="Append">Whether the action appends an event rather than issuing a command.</param>
/// <param name="Scope">What the call is read against.</param>
internal sealed record SpecificationActionStep(Step Step, bool Append, SpecificationStepScope Scope);
