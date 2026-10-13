// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.Reactors;

/// <summary>
/// An ordered refusal selector and its invocation-local production.
/// </summary>
/// <param name="Selector">The refusal selector.</param>
/// <param name="Handler">The event production or acknowledgment.</param>
internal record RefusalBranch(Refused Selector, Func<CommandRefusal, object?> Handler);
