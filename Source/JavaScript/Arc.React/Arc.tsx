// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CommandScope } from './commands';
import { IdentityProvider } from './identity';
import { Bindings } from './Bindings';
import { ArcConfiguration, ArcContext } from './ArcContext';
import { GetHttpHeaders } from '@cratis/arc';
import { QueryTransportMethod, QueryInstanceCache } from '@cratis/arc/queries';
import { QueryInstanceCacheContext } from './queries/QueryInstanceCacheContext';
import { useRef } from 'react';

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
     * Only applies when {@link ArcProps.queryTransportMethod} is a centralised hub transport.
     * Defaults to 1.
     */
    queryConnectionCount?: number;
}

/**
 * Arc context component.
 * @param props Props for configuring Arc
 */
export const Arc = (props: ArcProps) => {
    const configuration: ArcConfiguration = {
        microservice: props.microservice ?? '',
        development: props.development ?? false,
        origin: props.origin ?? '',
        basePath: props.basePath ?? '',
        apiBasePath: props.apiBasePath ?? '',
        httpHeadersCallback: props.httpHeadersCallback,
        queryTransportMethod: props.queryTransportMethod ?? QueryTransportMethod.ServerSentEvents,
        queryConnectionCount: props.queryConnectionCount ?? 1,
    };

    Bindings.initialize(
        configuration.microservice,
        configuration.apiBasePath,
        configuration.origin,
        configuration.httpHeadersCallback,
        configuration.queryTransportMethod,
        configuration.queryConnectionCount);

    // The cache is application-scoped — create once per Arc mount.
    const queryInstanceCache = useRef(new QueryInstanceCache());

    return (
        <ArcContext.Provider value={configuration}>
            <QueryInstanceCacheContext.Provider value={queryInstanceCache.current}>
                <IdentityProvider httpHeadersCallback={props.httpHeadersCallback}>
                    <CommandScope>
                        {props.children}
                    </CommandScope>
                </IdentityProvider>
            </QueryInstanceCacheContext.Provider>
        </ArcContext.Provider>);
};