// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Paging } from '../Paging';
import { Sorting } from '../Sorting';
import { BuildQueryHttpRequestOptions } from '../QueryHttpRequest';

/**
 * Builds a minimal set of options for exercising {@link executeQueryHttpRequest}.
 * @param overrides Optional property overrides (e.g. a different origin).
 */
export function makeOptions(overrides?: Partial<BuildQueryHttpRequestOptions>): BuildQueryHttpRequestOptions {
    return {
        route: '/api/test',
        apiBasePath: '',
        origin: 'https://api.example.com',
        args: {},
        parameterValues: {},
        paging: Paging.noPaging,
        sorting: Sorting.none,
        headers: {},
        ...overrides
    };
}
