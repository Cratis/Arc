// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { GetHttpHeaders, Globals } from '@cratis/arc';
import { QueryTransportMethod } from '@cratis/arc/queries';
import React from 'react';

export interface ArcConfiguration {
    microservice: string;
    development?: boolean
    origin?: string;
    basePath?: string;
    apiBasePath?: string;
    httpHeadersCallback?: GetHttpHeaders;
    queryTransportMethod?: QueryTransportMethod;
}

export const ArcContext = React.createContext<ArcConfiguration>({
    microservice: Globals.microservice,
    development: false,
    origin: '',
    basePath: '',
    apiBasePath: '',
    httpHeadersCallback: () => ({}),
    queryTransportMethod: QueryTransportMethod.ServerSentEvents,
});
