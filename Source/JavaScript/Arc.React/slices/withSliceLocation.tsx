// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ReactElement, ReactNode, createElement, useContext, useMemo } from 'react';
import { SliceLocation } from '@cratis/arc/reflection';
import { SliceScope, SliceScopeKind } from './SliceScope.js';
import { SliceScopeContext } from './SliceScopeContext.js';

interface FolderSliceProps {
    location: SliceLocation;
    children?: ReactNode;
}

const FolderSlice = (props: FolderSliceProps) => {
    const parent = useContext(SliceScopeContext);
    const { module, feature, slice } = props.location;

    const scope = useMemo(
        () => new SliceScope(parent, new SliceLocation(module, feature, slice), SliceScopeKind.folder),
        [parent, module, feature, slice]);

    return (
        <SliceScopeContext.Provider value={scope}>
            {props.children}
        </SliceScopeContext.Provider>
    );
};

/**
 * Wraps what a component renders in a slice scope for the slice folder the component lives in.
 * @remarks
 * The Arc Vite slice location plugin wraps what exported components in slice folders return with this, so their
 * subtree knows its slice without anything being passed. Slice locations of commands and queries used within it refine
 * the folder location when they lie on the same path, for instance a slice below a feature folder.
 * @param location The slice location of the folder.
 * @param children What the component renders.
 * @returns The element to render instead.
 */
export function withSliceLocation(location: SliceLocation, children: ReactNode): ReactElement {
    return createElement(FolderSlice, { location, children });
}
