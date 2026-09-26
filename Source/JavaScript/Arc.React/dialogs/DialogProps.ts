// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CloseDialog } from './CloseDialog.js';

/**
 * Represents the properties for a dialog component, including a method to close the dialog.
 */
export interface DialogProps<TResponse = object> {
    /**
     * A function to close the dialog, which takes a result and an optional response.
     */
    closeDialog: CloseDialog<TResponse>;
}
