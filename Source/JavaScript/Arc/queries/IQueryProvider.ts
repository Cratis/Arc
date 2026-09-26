// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Constructor } from '@cratis/fundamentals';
import { IQuery } from './IQuery.js';

/**
 * Defines the provider for queries.
 */
export abstract class IQueryProvider {

    /**
     * Gets a new instance of a specific query type.
     * @param {Constructor} queryType Type of query to get an instance of.
     */
    abstract get<T extends IQuery>(queryType: Constructor<T>): T;
}