// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ReactNode, useContext, useMemo } from 'react';
import { SliceLocation } from '@cratis/arc/reflection';
import { SliceScope, SliceScopeKind } from './SliceScope.js';
import { SliceScopeContext } from './SliceScopeContext.js';

/**
 * Properties for the {@link Slice} component.
 */
export interface SliceProps {
    /**
     * The slice location to use for everything rendered within. Wins over module, feature and slice.
     */
    location?: SliceLocation;

    /**
     * The module to use for everything rendered within.
     */
    module?: string;

    /**
     * The feature to use for everything rendered within.
     */
    feature?: string;

    /**
     * The slice to use for everything rendered within.
     */
    slice?: string;

    children?: ReactNode;
}

/**
 * Starts a new slice scope for everything rendered within it.
 * @remarks
 * With a location, or any of module, feature and slice, it is an explicit override: `useSliceContext()` within it
 * returns exactly that location, and parts not given are empty. Without any, it is a boundary: the slice locations
 * of commands and queries used within it stay within it, and it falls back to the enclosing scope when none are.
 * @param props The {@link SliceProps}.
 */
export const Slice = (props: SliceProps) => {
    const parent = useContext(SliceScopeContext);
    const isExplicit = props.location !== undefined || props.module !== undefined || props.feature !== undefined || props.slice !== undefined;
    const module = props.location?.module ?? props.module ?? '';
    const feature = props.location?.feature ?? props.feature ?? '';
    const slice = props.location?.slice ?? props.slice ?? '';

    const scope = useMemo(
        () => isExplicit
            ? new SliceScope(parent, new SliceLocation(module, feature, slice), SliceScopeKind.explicit)
            : new SliceScope(parent),
        [parent, isExplicit, module, feature, slice]);

    return (
        <SliceScopeContext.Provider value={scope}>
            {props.children}
        </SliceScopeContext.Provider>
    );
};
