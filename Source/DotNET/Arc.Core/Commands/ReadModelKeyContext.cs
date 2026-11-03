// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;

namespace Cratis.Arc.Commands;

/// <summary>The immutable input to a caller-derived read-model key provider.</summary>
/// <param name="Caller">The selected caller captured before context providers run.</param>
/// <param name="ReadModelType">The target read-model type.</param>
public sealed record ReadModelKeyContext(CommandCaller Caller, Type ReadModelType);
