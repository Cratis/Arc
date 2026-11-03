// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useContext } from 'react';
import { SliceLocation } from '@cratis/arc/reflection';
import { ApplicationLayerContext } from '../applicationContext/ApplicationLayerContext.js';
import { useSliceContext } from '../slices/useSliceContext.js';

/**
 * Use the slice location of the dialog the component is rendered in, for instance in dialog chrome such as a header.
 * @remarks
 * Resolved from the dialog's scope: an explicit location of the dialog wins, otherwise the location of where the
 * dialog was opened, refined by commands and queries used in the dialog.
 * @returns The {@link SliceLocation} of the dialog; {@link SliceLocation.empty} when not rendered in a dialog.
 */
export function useDialogSliceLocation(): SliceLocation {
    const inDialog = useContext(ApplicationLayerContext) !== undefined;
    const location = useSliceContext();
    return inDialog ? location : SliceLocation.empty;
}
