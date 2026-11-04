// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SliceScope } from '../slices/SliceScope.js';

/**
 * A live dialog registration that can be updated and removed.
 */
export interface ApplicationDialogRegistration {
    /**
     * Replaces the title and scope of the dialog in place. The dialog keeps its position in the stack, so the order
     * of the open dialogs and the focus do not change.
     * @param dialog The new title and the scope the dialog content renders in.
     */
    update(dialog: { title?: string, scope: SliceScope }): void;

    /**
     * Removes the dialog. Removing a dialog that is already removed does nothing.
     */
    remove(): void;
}
