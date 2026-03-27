// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { GetHttpHeaders } from './GetHttpHeaders';

/**
 * Represents the concept of something that can be configured.
 */
export interface ICanBeConfigured {
    /**
     * Set the microservice to be used for the query. This is passed along to the server to identify the microservice.
     * @param microservice Name of microservice
     */
    setMicroservice(microservice: string);

    /**
     * Set the base path for the API to use for the query. This is used to prepend to the path to any fetch operation.
     * @param apiBasePath Base path for the API
     */
    setApiBasePath(apiBasePath: string): void;

    /**
     * Set the origin to be used for the query. This is used to set the origin (scheme + host + port) for API calls.
     * @param origin The origin to use.
     */
    setOrigin(origin: string): void;

    /**
     * Set the callback to call for getting additional Http headers for Web requests.
     * @param callback Callback that gets called.
     */
    setHttpHeadersCallback(callback: GetHttpHeaders): void;
}
