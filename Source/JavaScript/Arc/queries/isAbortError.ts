// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Determines whether an error is the rejection produced by aborting a request through an
 * {@link AbortSignal} - as opposed to a genuine transport failure.
 *
 * Deliberately kept out of the `queries` barrel: it is an internal detail shared by the query
 * transport and {@link QueryFor}, not public surface for consumers to depend on.
 * @param error The error to inspect.
 * @returns True if the error represents an aborted request, false otherwise.
 */
export function isAbortError(error: unknown): boolean {
    return (error as { name?: string })?.name === 'AbortError';
}
