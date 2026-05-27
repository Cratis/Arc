// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Observable } from 'rxjs';
import { ObservableQueryDiagnosticsSnapshot } from './ObservableQueryDiagnosticsSnapshot';

/**
 * Contract for the observable query diagnostics service.
 *
 * The service aggregates runtime state from the query cache, the multiplexer connection pool,
 * and (optionally) component ownership maps into a single snapshot. Snapshots can be polled
 * via {@link getSnapshot} or observed through {@link snapshots$}.
 */
export interface IObservableQueryDiagnostics {
    /**
     * Returns the current diagnostics snapshot.
     */
    getSnapshot(): ObservableQueryDiagnosticsSnapshot;

    /**
     * Stream of diagnostics snapshots.
     */
    readonly snapshots$: Observable<ObservableQueryDiagnosticsSnapshot>;

    /**
     * Associates a human-readable owner label with a cache key.
     * Used by opt-in hook options (`owner?: string`) to identify which component
     * is using which query instance.
     * @param cacheKey The cache key to tag.
     * @param owner A descriptive label for the owning component (e.g. component name).
     */
    beginTracking(cacheKey: string, owner: string): void;

    /**
     * Removes the ownership association for the given cache key.
     * Should be called when the component unmounts or the query hook cleans up.
     * @param cacheKey The cache key to un-tag.
     */
    endTracking(cacheKey: string): void;
}
