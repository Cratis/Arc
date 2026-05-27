// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CommandScope } from './commands';
import { IdentityProvider } from './identity';
import { Bindings } from './Bindings';
import { ArcConfiguration, ArcContext } from './ArcContext';
import { GetHttpHeaders, Globals, ObservableQueryTransferMode } from '@cratis/arc';
import { QueryTransportMethod, QueryInstanceCache } from '@cratis/arc/queries';
import { resetSharedMultiplexer } from '@cratis/arc/queries';
import { ObservableQueryDiagnostics, getSharedMultiplexer } from '@cratis/arc/queries';
import { Messenger } from '@cratis/arc/messaging';
import { QueryInstanceCacheContext } from './queries/QueryInstanceCacheContext';
import { MessengerScopeContext } from './messaging/MessengerScopeContext';
import { useRef, useEffect, useState, useCallback } from 'react';

/**
 * Properties for the Arc context component.
 */
export interface ArcProps {
    children?: JSX.Element | JSX.Element[];
    microservice?: string;
    development?: boolean;
    origin?: string;
    basePath?: string;
    apiBasePath?: string;
    httpHeadersCallback?: GetHttpHeaders;
    /**
     * The transport method used for observable query subscriptions.
     * Defaults to {@link QueryTransportMethod.ServerSentEvents}.
     */
    queryTransportMethod?: QueryTransportMethod;
    /**
     * Number of hub connections maintained for observable queries.
     * When greater than one, queries are distributed across the pool round-robin.
     * Only applies when {@link ArcProps.queryTransportMethod} is a centralized hub transport.
     * Defaults to 1.
     */
    queryConnectionCount?: number;
    /**
     * When true, observable queries connect directly to the per-query WebSocket URL
     * instead of routing through the centralized hub endpoint.
     * Defaults to false (use the centralized hub).
     */
    queryDirectMode?: boolean;
    /**
     * Controls how observable query updates are transferred and exposed.
     * {@link ObservableQueryTransferMode.Delta} (default) computes per-update deltas for
     * use with the {@code useChangeStream} hook. {@link ObservableQueryTransferMode.Full}
     * delivers the whole collection on every update.
     */
    observableQueryTransferMode?: ObservableQueryTransferMode;
    /**
     * How long in milliseconds to retain a query cache entry after the last subscriber
     * releases it before evicting the subscription and cached data.
     *
     * A non-zero value lets users navigate away and back quickly without seeing a loading
     * flash — cached data is shown immediately while the fresh subscription re-establishes
     * in the background.  Defaults to {@link Globals.queryCacheRetentionMs} (30 000 ms).
     * Set to 0 to restore immediate eviction.
     */
    queryCacheRetentionMs?: number;
}

/**
 * Arc context component.
 * @param props Props for configuring Arc
 */
export const Arc = (props: ArcProps) => {
    const [queryVersion, setQueryVersion] = useState(0);
    const messenger = useRef(new Messenger());

    // The cache is application-scoped — create once per Arc mount.
    // Dispose is always deferred so React StrictMode re-mounts in any build environment
    // can cancel it — preventing the synthetic unmount from destroying entries that child
    // effects are about to re-acquire.
    const queryInstanceCache = useRef(new QueryInstanceCache(props.queryCacheRetentionMs ?? Globals.queryCacheRetentionMs));

    const observableQueryDiagnostics = useRef(new ObservableQueryDiagnostics(
        queryInstanceCache.current,
        () => getSharedMultiplexer(),
        () => ({
            queryTransportMethod: props.queryTransportMethod ?? QueryTransportMethod.ServerSentEvents,
            queryDirectMode: props.queryDirectMode ?? false,
        }),
    ));

    const reconnectQueries = useCallback(() => {
        queryInstanceCache.current.teardownAllSubscriptions();
        resetSharedMultiplexer();
        setQueryVersion(v => v + 1);
    }, []);

    const configuration: ArcConfiguration = {
        microservice: props.microservice ?? '',
        messenger: messenger.current,
        development: props.development ?? false,
        origin: props.origin ?? '',
        basePath: props.basePath ?? '',
        apiBasePath: props.apiBasePath ?? '',
        httpHeadersCallback: props.httpHeadersCallback,
        queryTransportMethod: props.queryTransportMethod ?? QueryTransportMethod.ServerSentEvents,
        queryConnectionCount: props.queryConnectionCount ?? 1,
        queryDirectMode: props.queryDirectMode ?? false,
        observableQueryTransferMode: props.observableQueryTransferMode ?? ObservableQueryTransferMode.Delta,
        queryCacheRetentionMs: props.queryCacheRetentionMs ?? Globals.queryCacheRetentionMs,
        queryVersion,
        reconnectQueries,
        observableQueryDiagnostics: observableQueryDiagnostics.current,
    };

    Bindings.initialize(
        configuration.microservice,
        configuration.apiBasePath,
        configuration.origin,
        configuration.httpHeadersCallback,
        configuration.queryTransportMethod,
        configuration.queryConnectionCount,
        configuration.queryDirectMode,
        configuration.observableQueryTransferMode);

    useEffect(() => {
        const cache = queryInstanceCache.current;
        cache.cancelPendingDispose();
        return () => {
            // Always defer so React StrictMode re-mounts in any build environment can
            // cancel the dispose before the timeout fires.
            cache.deferDispose();
        };
    }, []);

    return (
        <ArcContext.Provider value={configuration}>
            <MessengerScopeContext.Provider value={configuration.messenger}>
                <QueryInstanceCacheContext.Provider value={queryInstanceCache.current}>
                    <IdentityProvider httpHeadersCallback={props.httpHeadersCallback}>
                        <CommandScope>
                            {props.children}
                        </CommandScope>
                    </IdentityProvider>
                </QueryInstanceCacheContext.Provider>
            </MessengerScopeContext.Provider>
        </ArcContext.Provider>);
};
