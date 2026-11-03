// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Commands;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// The typed delegates closed over one read model.
/// </summary>
/// <param name="ResolveProtected">Resolves the protected decision read.</param>
/// <param name="ResolveStream">Resolves the per-stream read.</param>
/// <param name="Describe">Describes the decision read.</param>
/// <param name="Refusal">Checks decision-read admission for the declared scope.</param>
/// <param name="ResolveProtectedAsync">Resolves the protected decision read without blocking.</param>
/// <param name="ResolveStreamAsync">Resolves the per-stream read without blocking.</param>
/// <param name="ResolveTokenAsync">Issues the <c>DecisionRead&lt;T&gt;</c> token without blocking.</param>
/// <param name="ReleaseAsync">Releases compliance-protected values without blocking.</param>
internal sealed record ReadModelDispatchEntry(
    Func<IServiceProvider, CommandContext, ReadModelKey, object?> ResolveProtected,
    Func<CommandContext, IServiceProvider, ParameterInfo?, object?> ResolveStream,
    Func<IDecisionReads, DecisionReadDescription> Describe,
    Func<IDecisionReads, bool, DecisionReadRefusalReason?> Refusal,
    Func<IServiceProvider, CommandContext, ReadModelKey, ValueTask<object?>> ResolveProtectedAsync,
    Func<CommandContext, IServiceProvider, ParameterInfo?, ValueTask<object?>> ResolveStreamAsync,
    Func<IServiceProvider, ValueTask<object>> ResolveTokenAsync,
    Func<IReadModels, object, Task<object>> ReleaseAsync);
