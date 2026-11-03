// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Identifies the screen the user is looking at, as contributed by the host or router.
 */
export interface ApplicationScreen {
    /**
     * A stable identifier of the screen. Empty when nobody contributes one.
     */
    readonly id: string;

    /**
     * The human readable name of the screen. Empty when nobody contributes one.
     */
    readonly name: string;

    /**
     * The path, route or URL of the screen. Empty when nobody contributes one.
     */
    readonly path: string;
}

/**
 * The screen used when nobody contributes one.
 */
export const emptyApplicationScreen: ApplicationScreen = Object.freeze({ id: '', name: '', path: '' });
