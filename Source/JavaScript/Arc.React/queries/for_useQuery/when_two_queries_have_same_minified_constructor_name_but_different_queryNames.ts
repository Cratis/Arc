// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render, waitFor } from '@testing-library/react';
import sinon from 'sinon';
import { useQuery } from '../useQuery';
import { QueryFor, QueryInstanceCache } from '@cratis/arc/queries';
import { ParameterDescriptor } from '@cratis/arc/reflection';
import { ArcContext, ArcConfiguration } from '../../ArcContext';
import { QueryInstanceCacheContext } from '../QueryInstanceCacheContext';
import { createFetchHelper } from '@cratis/arc/helpers/fetchHelper';

/* eslint-disable @typescript-eslint/no-explicit-any */

interface Item {
    id: string;
    name: string;
}

class FirstQuery extends QueryFor<Item[]> {
    readonly route = '/api/first-query';
    readonly queryName = 'Namespace.First.Query';
    readonly parameterDescriptors: ParameterDescriptor[] = [];

    get requiredRequestParameters(): string[] {
        return [];
    }

    defaultValue: Item[] = [];

    constructor() {
        super(Object, true);
    }
}

class SecondQuery extends QueryFor<Item[]> {
    readonly route = '/api/second-query';
    readonly queryName = 'Namespace.Second.Query';
    readonly parameterDescriptors: ParameterDescriptor[] = [];

    get requiredRequestParameters(): string[] {
        return [];
    }

    defaultValue: Item[] = [];

    constructor() {
        super(Object, true);
    }
}

// Simulate minification — both classes receive the same single-letter constructor name.
// Without the queryName fix, both hooks would build the same cache key ('a::') and
// share a cache entry, making both components fetch from the same URL.
Object.defineProperty(FirstQuery, 'name', { value: 'a', configurable: true });
Object.defineProperty(SecondQuery, 'name', { value: 'a', configurable: true });

describe('when two queries have same minified constructor name but different queryNames', () => {
    let fetchHelper: ReturnType<typeof createFetchHelper>;
    let fetchStub: sinon.SinonStub;
    const calledUrls: string[] = [];

    const config: ArcConfiguration = {
        microservice: '',
        apiBasePath: '',
        origin: '',
    };

    beforeEach(async () => {
        calledUrls.length = 0;

        fetchHelper = createFetchHelper();
        fetchStub = fetchHelper.stubFetch();

        fetchStub.callsFake((url: unknown) => {
            const urlString = String(url);
            calledUrls.push(urlString);
            const data = urlString.includes('first-query')
                ? [{ id: '1', name: 'From First' }]
                : [{ id: '2', name: 'From Second' }];
            return Promise.resolve({
                json: async () => ({
                    data,
                    isSuccess: true,
                    isAuthorized: true,
                    isValid: true,
                    hasExceptions: false,
                    validationResults: [],
                    exceptionMessages: [],
                    exceptionStackTrace: '',
                    paging: { page: 0, size: 0, totalItems: 1, totalPages: 1 }
                })
            } as Response);
        });

        const cache = new QueryInstanceCache(0);

        render(
            React.createElement(
                QueryInstanceCacheContext.Provider,
                { value: cache },
                React.createElement(
                    ArcContext.Provider,
                    { value: config },
                    React.createElement(() => { useQuery(FirstQuery); return null; }),
                    React.createElement(() => { useQuery(SecondQuery); return null; })
                )
            )
        );

        await waitFor(() => {
            if (fetchStub.callCount < 2) throw new Error('Waiting for both fetches');
        });
    });

    afterEach(() => {
        fetchHelper.restore();
    });

    it('should perform separate fetch requests for each query', () => {
        fetchStub.callCount.should.equal(2);
    });

    it('should fetch the first query from its own route', () => {
        calledUrls.some(url => url.includes('first-query')).should.be.true;
    });

    it('should fetch the second query from its own route', () => {
        calledUrls.some(url => url.includes('second-query')).should.be.true;
    });
});
