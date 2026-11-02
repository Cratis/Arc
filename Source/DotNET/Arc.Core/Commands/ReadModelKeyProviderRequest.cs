// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Commands;

/// <summary>
/// Framework-only context for resolving a provider; only the captured caller and model type reach the provider.
/// </summary>
/// <param name="ReadModelType">The requested model.</param>
/// <param name="CommandType">The command type for safe refusal logging.</param>
/// <param name="CurrentPrincipal">The observable current principal.</param>
/// <param name="Logger">The optional framework logger.</param>
internal readonly record struct ReadModelKeyProviderRequest(Type ReadModelType, Type CommandType, ClaimsPrincipal? CurrentPrincipal, ILogger? Logger = null);
