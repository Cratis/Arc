// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario;

/// <summary>
/// Event raised when a notification is sent to a recipient.
/// </summary>
/// <param name="Message">The notification message.</param>
[EventType("c93d45fa-b1a6-5e43-a7c5-3f0a9b82e456")]
public record NotificationSent(string Message);
