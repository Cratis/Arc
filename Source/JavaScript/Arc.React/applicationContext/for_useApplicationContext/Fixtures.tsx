// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ReactNode, useMemo } from 'react';
import { SliceLocation } from '@cratis/arc/reflection';
import { ArcContext } from '../../ArcContext.js';
import { ApplicationContext } from '../ApplicationContext.js';
import { SliceScope } from '../../slices/SliceScope.js';
import { SliceScopeContext } from '../../slices/SliceScopeContext.js';
import { useApplicationContext } from '../useApplicationContext.js';
import { useRegisterSliceLocation } from '../../slices/useRegisterSliceLocation.js';

export const Host = (props: { store: ApplicationContext, sliceMarkers?: boolean, children?: ReactNode }) => {
    const root = useMemo(() => new SliceScope(), []);
    const store = props.store;
    useMemo(() => store.registerScope(root), [store, root]);
    return (
        <ArcContext.Provider value={{ microservice: '', applicationContext: store, sliceMarkers: props.sliceMarkers }}>
            <SliceScopeContext.Provider value={root}>
                {props.children}
            </SliceScopeContext.Provider>
        </ArcContext.Provider>
    );
};

export const TopBar = (props: { onRender?: (snapshot: ReturnType<typeof useApplicationContext>) => void }) => {
    const snapshot = useApplicationContext();
    props.onRender?.(snapshot);
    return <span data-testid="top-bar">{snapshot.location.toString()}</span>;
};

export const UsesSlice = (props: { location: SliceLocation }) => {
    useRegisterSliceLocation(props.location);
    return <button data-testid="inside">inside</button>;
};
