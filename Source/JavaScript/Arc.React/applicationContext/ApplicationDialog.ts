// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SliceLocation } from '@cratis/arc/reflection';

/**
 * Describes a dialog that is open.
 */
export interface ApplicationDialog {
    /**
     * A unique identifier of the open dialog.
     */
    readonly id: string;

    /**
     * The title of the dialog, when it declared one.
     */
    readonly title?: string;

    /**
     * The resolved slice location of the dialog.
     */
    readonly location: SliceLocation;
}
