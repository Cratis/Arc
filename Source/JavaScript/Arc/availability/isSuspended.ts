// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Suspended } from './AvailabilityReason.js';

/**
 * Determines whether a command or query result was refused because an operator has suspended its part of the
 * application. It is not the same as {@link isNotAvailable}: a suspended part comes back when the suspension is lifted.
 * @param result The result of a command or a query.
 * @returns True when the part is suspended.
 */
export function isSuspended(result: { readonly isAuthorized: boolean; readonly authorizationFailureReason?: string }): boolean {
    return !result.isAuthorized && result.authorizationFailureReason === Suspended;
}
