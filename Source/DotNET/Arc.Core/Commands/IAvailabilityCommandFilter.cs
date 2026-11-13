// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// Defines a command filter that decides whether a command is available, before authorization.
/// </summary>
/// <remarks>
/// It is an <see cref="IAuthorizationCommandFilter"/>, so it runs before ordinary filters, and the command filters run it
/// before the other authorization filters: a command in a part that is not available is refused with
/// <see cref="Availability.AvailabilityReason.NotAvailable"/> before <c>authorize</c> is evaluated.
/// </remarks>
public interface IAvailabilityCommandFilter : IAuthorizationCommandFilter;
