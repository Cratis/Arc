// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect } from 'react';
import type { QueryResultWithState } from '@cratis/arc/queries';
import {
    type ObservableSuspenseResource,
    claimResource,
    reconnectResource,
    releaseResource,
} from './SuspenseObservableQueryResources.js';

/** Commits ownership separately from reconnects so version changes cannot release mounted consumers. */
export function useSuspenseObservableResource<T>(
    resource: ObservableSuspenseResource<T> | undefined,
    options: { cacheKey: string; isEnabled: boolean; queryVersion?: number },
    handleUpdate: (value: QueryResultWithState<T>) => void,
): void {
    const { cacheKey, isEnabled, queryVersion } = options;
    useEffect(() => {
        if (!isEnabled) {
            return;
        }
        if (resource === undefined) {
            return;
        }
        if (!claimResource(resource)) {
            return;
        }
        const listener = (value: QueryResultWithState<T>) => handleUpdate(value);
        resource.listeners.add(listener);
        return () => {
            resource.listeners.delete(listener);
            releaseResource(resource);
        };
    }, [cacheKey, resource, isEnabled, handleUpdate]);

    useEffect(() => {
        if (!isEnabled) {
            return;
        }
        if (resource !== undefined) {
            reconnectResource(resource, queryVersion ?? 0);
        }
    }, [queryVersion, resource, isEnabled]);
}
