// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryConnectionDescriptor } from '../../ObservableQueryConnectionFactory.js';
import { Globals } from '../../../Globals.js';
import { QueryTransportMethod } from '../../QueryTransportMethod.js';

export class a_descriptor {
    descriptor: QueryConnectionDescriptor;
    originalDirectMode: boolean;
    originalTransportMethod: QueryTransportMethod;

    constructor() {
        this.originalDirectMode = Globals.queryDirectMode;
        this.originalTransportMethod = Globals.queryTransportMethod;

        this.descriptor = {
            route: '/api/test/{id}',
            queryName: 'TestApp.Features.Queries.AllItems',
            origin: 'https://example.com',
            apiBasePath: '',
            microservice: '',
            args: { id: 'item-42' },
        };
    }
}
