// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Constructor } from '@cratis/fundamentals';
import { IQueryProvider } from './IQueryProvider.js';
import { IQuery } from './IQuery.js';
import { GetHttpHeaders } from '../GetHttpHeaders.js';

/**
 * Represents an implementation of {@link IQueryProvider}
 */
export class QueryProvider implements IQueryProvider {

    /**
     * Initializes a new instance of {@link QueryProvider}
     * @param _microservice Name of the microservice to provide queries for.
     * @param _apiBasePath Base path for the API to use for the query.
     * @param _origin Origin to use for the query.
     * @param _httpHeadersCallback Callback to get HTTP headers for the query.
     */
    constructor(
        private readonly _microservice: string,
        private readonly _apiBasePath: string,
        private readonly _origin: string,
        private readonly _httpHeadersCallback: GetHttpHeaders) { }

    /** @inheritdoc */
    get<T extends IQuery>(queryType: Constructor<T>): T {
        const query = new queryType();
        query.setMicroservice(this._microservice);
        query.setApiBasePath(this._apiBasePath);
        query.setOrigin(this._origin);
        query.setHttpHeadersCallback(this._httpHeadersCallback);
        return query;
    }
}