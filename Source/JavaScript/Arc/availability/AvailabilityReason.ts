// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * The authorization failure reason of a command or query result when its part of the application is not available
 * to the caller. It is distinct from being forbidden: availability decides whether a part exists for the caller at
 * all, authorization decides what the caller may do in it.
 */
export const NotAvailable = 'NotAvailable';
