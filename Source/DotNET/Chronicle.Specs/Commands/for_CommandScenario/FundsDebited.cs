// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario;

/// <summary>
/// Event raised when funds are debited from an account.
/// </summary>
/// <param name="Amount">The amount debited.</param>
[EventType("a69b23d8-f9e4-4c21-b5a3-1d8e7f60c234")]
public record FundsDebited(decimal Amount);
