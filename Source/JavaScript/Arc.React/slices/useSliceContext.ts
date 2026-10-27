// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useContext, useSyncExternalStore } from 'react';
import { SliceLocation } from '@cratis/arc/reflection';
import { SliceScopeContext } from './SliceScopeContext.js';

const subscribeToNothing = () => () => {
    // Without a slice scope there is nothing that can change.
};
const getEmpty = () => SliceLocation.empty;

/**
 * Use the slice context the component is rendered in: the module, feature and slice it belongs to.
 * @remarks
 * Resolved from the nearest slice scope, without the component passing anything. An explicit `<Slice>` override
 * wins, then the location of the slice folder the rendering component lives in (when the Arc Vite slice location
 * plugin is used), then the slice locations of the commands and queries used in the same scope.
 * @returns The {@link SliceLocation}; {@link SliceLocation.empty} when nothing is known. Parts are never null.
 */
export function useSliceContext(): SliceLocation {
    const scope = useContext(SliceScopeContext);
    const subscribe = scope?.subscribe ?? subscribeToNothing;
    const getSnapshot = scope?.getSnapshot ?? getEmpty;
    return useSyncExternalStore(subscribe, getSnapshot, getSnapshot);
}
