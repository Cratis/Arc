// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/// <reference types="vite/client" />

const developmentBuild = import.meta.env?.DEV === true;
let developmentOverride: boolean | undefined;

/**
 * Whether command form development diagnostics should be emitted.
 * @returns True for development builds unless a spec override is active.
 */
export function shouldEmitCommandFormDevelopmentWarnings(): boolean {
    return developmentOverride ?? developmentBuild;
}

/**
 * Overrides command form development diagnostics for a spec without changing process-wide state.
 * @param value True or false to override the build environment; undefined to restore it.
 */
export function setCommandFormDevelopmentWarningsForTesting(
    value: boolean | undefined,
): void {
    developmentOverride = value;
}
