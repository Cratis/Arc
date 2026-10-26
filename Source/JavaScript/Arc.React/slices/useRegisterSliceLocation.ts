// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useContext, useEffect } from 'react';
import { SliceLocation } from '@cratis/arc/reflection';
import { SliceScopeContext } from './SliceScopeContext.js';

/**
 * Register a slice location as in use on the nearest slice scope for as long as the component is mounted.
 * @remarks
 * Command and query hooks call this with the slice location of their proxy, so other components rendered in the
 * same scope, including shared ones, inherit it. Call it from custom hooks that use commands or queries some other way.
 * @param location The slice location to register. An empty location registers nothing.
 */
export function useRegisterSliceLocation(location: SliceLocation): void {
    const scope = useContext(SliceScopeContext);
    const { module, feature, slice } = location;

    useEffect(() => scope?.register(new SliceLocation(module, feature, slice)), [scope, module, feature, slice]);
}
