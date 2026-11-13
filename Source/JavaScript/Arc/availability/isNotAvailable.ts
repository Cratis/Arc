// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { NotAvailable } from './AvailabilityReason.js';

/**
 * Determines whether a command or query result was refused because its part of the application is not available to
 * the caller. This is not the same as being forbidden: the caller is not told whether it would have been allowed.
 * @param result The result of a command or a query.
 * @returns True when the part is not available.
 */
export function isNotAvailable(result: { readonly isAuthorized: boolean; readonly authorizationFailureReason?: string }): boolean {
    return !result.isAuthorized && result.authorizationFailureReason === NotAvailable;
}
