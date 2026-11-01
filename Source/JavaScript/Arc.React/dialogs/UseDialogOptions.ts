// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Options for {@link useDialog}.
 */
export interface UseDialogOptions {
    /**
     * An explicit slice location for the dialog. Wins over the location inherited from where the dialog was opened.
     * A `SliceLocation` can be given; parts not given are empty.
     */
    slice?: { module?: string, feature?: string, slice?: string };

    /**
     * A title for the dialog, reported in the application context's open dialogs.
     */
    title?: string;

    /**
     * Whether the open dialog is registered in the application context, so it gets focus and is listed in its dialogs.
     * Defaults to true.
     */
    trackInApplicationContext?: boolean;
}
