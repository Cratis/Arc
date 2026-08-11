// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryFor, QueryResult } from '@cratis/arc/queries';
import { ParameterDescriptor } from '@cratis/arc/reflection';
import { FakeQueryResult } from './FakeQuery';

/**
 * A query that rejects with a nullish value rather than an {@link Error}. `IQueryFor` only promises a
 * rejection, not what it rejects with - a thrown `undefined` or a bare `Promise.reject()` from a custom
 * implementation, an interceptor or a polyfilled transport all arrive at the hook this way.
 */
export class FakeNullishRejectingQuery extends QueryFor<FakeQueryResult[]> {
    readonly route = '/api/fake-nullish-rejecting-query';
    readonly parameterDescriptors: ParameterDescriptor[] = [];

    get requiredRequestParameters(): string[] {
        return [];
    }

    defaultValue: FakeQueryResult[] = [];

    constructor() {
        super(Object, true);
    }

    override perform(): Promise<QueryResult<FakeQueryResult[]>> {
        return Promise.reject(undefined);
    }
}
