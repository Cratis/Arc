// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React, { ReactNode, Suspense } from 'react';
import { QueryErrorBoundary, QueryErrorBoundaryProps } from './QueryErrorBoundary';

/**
 * Props for {@link QueryBoundary}.
 */
export interface QueryBoundaryProps extends QueryErrorBoundaryProps {
    /**
     * The fallback rendered by the inner `<Suspense>` while queries are loading.
     * Defaults to `null` (no visible loading state) when omitted.
     */
    loadingFallback?: ReactNode;
}

/**
 * Convenience wrapper that combines `<Suspense>` and {@link QueryErrorBoundary} in the correct order.
 *
 * ```tsx
 * <QueryBoundary
 *     loadingFallback={<Spinner />}
 *     onError={({ isQueryUnauthorized, reset }) =>
 *         isQueryUnauthorized
 *             ? <p>Not authorized. <button onClick={reset}>Retry</button></p>
 *             : <p>Something went wrong. <button onClick={reset}>Retry</button></p>
 *     }
 * >
 *     <ItemList />
 * </QueryBoundary>
 * ```
 *
 * `QueryBoundary` passes all remaining props to {@link QueryErrorBoundary}.
 * The `fallback` prop is forwarded to `QueryErrorBoundary` (error fallback),
 * while `loadingFallback` is forwarded to `<Suspense>` (loading state).
 */
export const QueryBoundary = ({ loadingFallback = null, children, ...errorBoundaryProps }: QueryBoundaryProps) => (
    <QueryErrorBoundary {...errorBoundaryProps}>
        <Suspense fallback={loadingFallback}>
            {children}
        </Suspense>
    </QueryErrorBoundary>
);
