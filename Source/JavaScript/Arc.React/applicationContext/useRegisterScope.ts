// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useContext, useEffect } from 'react';
import { SliceScope } from '../slices/SliceScope.js';
import { ApplicationLayerContext } from './ApplicationLayerContext.js';
import { useApplicationContextStore } from './useApplicationContextStore.js';

/**
 * Registers a slice scope with the application context, in the page layer or the layer of the dialog it renders in.
 * @param scope The scope to register.
 */
export function useRegisterScope(scope: SliceScope): void {
    const store = useApplicationContextStore();
    const layer = useContext(ApplicationLayerContext);

    useEffect(() => store?.registerScope(scope, layer), [store, scope, layer]);
}
