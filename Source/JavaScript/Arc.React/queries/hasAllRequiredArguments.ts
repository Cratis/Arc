// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Checks whether every required request parameter has a usable value in the given arguments.
 * @param {string[]} requiredRequestParameters Names of the required parameters.
 * @param {Record<string, unknown>} args Arguments supplied to the query, if any.
 * @returns {boolean} True when all required parameters are set (not undefined, null or empty string).
 */
export function hasAllRequiredArguments(
    requiredRequestParameters: string[],
    args?: Record<string, unknown>,
): boolean {
    if (requiredRequestParameters.length === 0) {
        return true;
    }

    return requiredRequestParameters.every((requiredRequestParameter) => {
        const value = args?.[requiredRequestParameter];
        return value !== undefined && value !== null && value !== '';
    });
}
