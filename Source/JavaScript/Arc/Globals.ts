// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export interface IGlobals {
    microservice: string;
    apiBasePath: string;
    origin: string;
    microserviceHttpHeader: string;
    microserviceWSQueryArgument: string;
}

export const Globals: IGlobals = {
    microservice: '',
    apiBasePath: '',
    origin: '',
    microserviceHttpHeader: 'x-cratis-microservice',
    microserviceWSQueryArgument: 'x-cratis-microservice'
};