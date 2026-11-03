// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SliceLocation } from '@cratis/arc/reflection';
import { ApplicationScreen } from './ApplicationScreen.js';
import { ApplicationDialog } from './ApplicationDialog.js';

/**
 * An immutable snapshot of what the user is looking at right now.
 * @remarks
 * The instance only changes identity when something in it changes.
 */
export interface ApplicationContextSnapshot {
    /**
     * The resolved location of the focused scope: the page, or the top-most open dialog.
     */
    readonly location: SliceLocation;

    /**
     * Every distinct slice location registered by mounted commands and queries in the focused scope, before folding.
     */
    readonly activeSlices: readonly SliceLocation[];

    /**
     * The screen contributed by the host or router; empty parts when nobody contributes one.
     */
    readonly screen: ApplicationScreen;

    /**
     * The stack of open dialogs, bottom first. The last one is focused.
     */
    readonly dialogs: readonly ApplicationDialog[];

    /**
     * Host specific values contributed through {@link ApplicationContextContribution.values}.
     */
    readonly contributions: Readonly<Record<string, unknown>>;
}
