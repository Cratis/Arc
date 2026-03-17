// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Component, ErrorInfo, ReactNode } from 'react';
import { QueryFailed } from './QueryFailed';
import { QueryUnauthorized } from './QueryUnauthorized';

/**
 * Render props passed to the `fallback` or `onError` callbacks.
 */
export interface QueryErrorInfo {
    /** The raw error that was caught. */
    readonly error: Error;
    /** `true` when the error is a {@link QueryFailed}. */
    readonly isQueryFailed: boolean;
    /** `true` when the error is a {@link QueryUnauthorized}. */
    readonly isQueryUnauthorized: boolean;
    /** Resets the boundary so the child subtree is re-mounted. */
    reset(): void;
}

/**
 * Props for {@link QueryErrorBoundary}.
 */
export interface QueryErrorBoundaryProps {
    children: ReactNode;

    /**
     * Called when the boundary catches an error.
     * Return a React node to render in place of the failed subtree,
     * or `undefined` / `null` to fall back to the default error UI.
     */
    onError?: (info: QueryErrorInfo) => ReactNode;

    /**
     * A React node to render when an error is caught.
     * Use this for simple static fallback UIs.
     * When both `fallback` and `onError` are provided, `onError` takes precedence.
     */
    fallback?: ReactNode;
}

interface QueryErrorBoundaryState {
    error: Error | null;
}

/**
 * A class-based error boundary that catches errors thrown by `useSuspenseQuery` and
 * `useSuspenseObservableQuery` — specifically {@link QueryFailed} and {@link QueryUnauthorized}.
 *
 * Place it around any subtree that uses Suspense query hooks:
 *
 * ```tsx
 * <QueryErrorBoundary onError={({ error, isQueryFailed, reset }) =>
 *     <div>
 *         <p>{isQueryFailed ? 'Server error' : 'Not authorized'}</p>
 *         <button onClick={reset}>Retry</button>
 *     </div>
 * }>
 *     <Suspense fallback={<Spinner />}>
 *         <MyComponent />
 *     </Suspense>
 * </QueryErrorBoundary>
 * ```
 */
export class QueryErrorBoundary extends Component<QueryErrorBoundaryProps, QueryErrorBoundaryState> {
    constructor(props: QueryErrorBoundaryProps) {
        super(props);
        this.state = { error: null };
    }

    static getDerivedStateFromError(error: Error): QueryErrorBoundaryState {
        return { error };
    }

    componentDidCatch(error: Error, info: ErrorInfo) {
        if (process.env.NODE_ENV !== 'test') {
            console.error('[QueryErrorBoundary]', error, info);
        }
    }

    reset = () => {
        this.setState({ error: null });
    };

    render() {
        const { error } = this.state;

        if (error === null) {
            return this.props.children;
        }

        const info: QueryErrorInfo = {
            error,
            isQueryFailed: error instanceof QueryFailed,
            isQueryUnauthorized: error instanceof QueryUnauthorized,
            reset: this.reset
        };

        if (this.props.onError) {
            return this.props.onError(info) ?? null;
        }

        if (this.props.fallback !== undefined) {
            return this.props.fallback;
        }

        return null;
    }
}
