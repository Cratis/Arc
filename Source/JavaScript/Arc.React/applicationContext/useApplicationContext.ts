// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useSyncExternalStore } from 'react';
import { ApplicationContext } from './ApplicationContext.js';
import { ApplicationContextSnapshot } from './ApplicationContextSnapshot.js';
import { useApplicationContextStore } from './useApplicationContextStore.js';

const fallback = new ApplicationContext();

/**
 * Use what the user is looking at right now: the focused location, active slices, screen, open dialogs and host
 * contributions. Readable from anywhere under `<Arc>`, also from components outside every slice scope.
 * @returns The immutable {@link ApplicationContextSnapshot}; empty when not rendered within `<Arc>`.
 */
export function useApplicationContext(): ApplicationContextSnapshot {
    const store = useApplicationContextStore() ?? fallback;
    return useSyncExternalStore(store.subscribe, store.getSnapshot, store.getSnapshot);
}
