// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryTransportMethod } from './queries/QueryTransportMethod';

export interface IGlobals {
    microservice: string;
    apiBasePath: string;
    origin: string;
    microserviceHttpHeader: string;
    microserviceWSQueryArgument: string;
    queryTransportMethod: QueryTransportMethod;
    /**
     * Number of hub connections maintained for observable queries.
     * When greater than one, queries are distributed across the pool round-robin.
     * Only applies when {@link queryTransportMethod} is a centralized hub transport.
     * Defaults to 1.
     */
    queryConnectionCount: number;
}

export const Globals: IGlobals = {
    microservice: '',
    apiBasePath: '',
    origin: '',
    microserviceHttpHeader: 'x-cratis-microservice',
    microserviceWSQueryArgument: 'x-cratis-microservice',
    queryTransportMethod: QueryTransportMethod.WebSocket,
    queryConnectionCount: 1,
};