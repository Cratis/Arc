// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ReactNode, useEffect, useMemo } from 'react';
import { SliceLocation } from '@cratis/arc/reflection';
import { SliceScope, SliceScopeKind } from '../slices/SliceScope.js';
import { SliceScopeContext } from '../slices/SliceScopeContext.js';
import { ApplicationLayerContext } from '../applicationContext/ApplicationLayerContext.js';
import { SliceMarker } from '../applicationContext/SliceMarker.js';
import { useApplicationContextStore } from '../applicationContext/useApplicationContextStore.js';

/**
 * Properties for {@link DialogSliceScope}.
 */
export interface DialogSliceScopeProps {
    id: string;
    opener: SliceScope | undefined;
    title?: string;
    slice?: { module?: string, feature?: string, slice?: string };
    track: boolean;
    children?: ReactNode;
}

/**
 * Renders the content of an open dialog in a slice scope that is a child of the scope it was opened from, and
 * registers the dialog with the application context.
 * @param props The {@link DialogSliceScopeProps}.
 */
export const DialogSliceScope = (props: DialogSliceScopeProps) => {
    const store = useApplicationContextStore();
    const { opener, slice } = props;
    const module = slice?.module ?? '';
    const feature = slice?.feature ?? '';
    const sliceName = slice?.slice ?? '';
    const explicit = slice !== undefined;

    const scope = useMemo(
        () => explicit
            ? new SliceScope(opener, new SliceLocation(module, feature, sliceName), SliceScopeKind.explicit)
            : new SliceScope(opener),
        [opener, explicit, module, feature, sliceName]);

    // The dialog's scope is registered as part of the dialog's own layer, so it is not mistaken for the page.
    useEffect(
        () => props.track ? store?.registerDialog({ id: props.id, title: props.title, scope }) : undefined,
        [store, scope, props.id, props.title, props.track]);

    return (
        <ApplicationLayerContext.Provider value={props.track ? props.id : undefined}>
            <SliceScopeContext.Provider value={scope}>
                <SliceMarker scope={scope}>
                    {props.children}
                </SliceMarker>
            </SliceScopeContext.Provider>
        </ApplicationLayerContext.Provider>
    );
};
