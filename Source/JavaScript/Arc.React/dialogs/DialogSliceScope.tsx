// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ReactNode, useEffect, useMemo, useRef } from 'react';
import { SliceLocation } from '@cratis/arc/reflection';
import { SliceScope, SliceScopeKind } from '../slices/SliceScope.js';
import { SliceScopeContext } from '../slices/SliceScopeContext.js';
import { ApplicationLayerContext } from '../applicationContext/ApplicationLayerContext.js';
import { ApplicationDialogRegistration } from '../applicationContext/ApplicationDialogRegistration.js';
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

    const latest = useRef({ title: props.title, scope });
    latest.current = { title: props.title, scope };
    const registration = useRef<ApplicationDialogRegistration | undefined>(undefined);

    // The dialog's scope is registered as part of the dialog's own layer, so it is not mistaken for the page.
    // The registration lives as long as the dialog is tracked; title and scope changes update it in place.
    useEffect(() => {
        if (!props.track || !store) {
            return undefined;
        }
        const handle = store.trackDialog({ id: props.id, ...latest.current });
        registration.current = handle;
        return () => {
            handle.remove();
            if (registration.current === handle) {
                registration.current = undefined;
            }
        };
    }, [store, props.id, props.track]);

    useEffect(() => {
        registration.current?.update({ title: props.title, scope });
    }, [props.title, scope]);

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
