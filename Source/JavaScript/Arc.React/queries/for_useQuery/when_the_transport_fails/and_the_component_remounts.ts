// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { renderHook, waitFor } from '@testing-library/react';
import sinon from 'sinon';
import { QueryInstanceCache, QueryResultWithState } from '@cratis/arc/queries';
import { createFetchHelper } from '@cratis/arc/helpers/fetchHelper';
import { useQuery } from '../../useQuery';
import { FakeQuery, FakeQueryResult } from '../FakeQuery';
import { ArcContext, ArcConfiguration } from '../../../ArcContext';
import { QueryInstanceCacheContext } from '../../QueryInstanceCacheContext';

describe('and the component remounts', () => {
    let fetchStub: sinon.SinonStub;
    let fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };
    let firstRenderAfterRemount: QueryResultWithState<unknown>;
    let settledResultAfterRemount: QueryResultWithState<unknown>;

    const config: ArcConfiguration = {
        microservice: 'test-microservice',
        apiBasePath: '/api',
        origin: 'https://example.com'
    };

    const successfulResponse = {
        json: async () => ({
            data: [{ id: '1', name: 'first' }],
            isSuccess: true,
            isAuthorized: true,
            isValid: true,
            hasExceptions: false,
            validationResults: [],
            exceptionMessages: [],
            exceptionStackTrace: '',
            paging: { page: 0, size: 0, totalItems: 0, totalPages: 0 }
        })
    } as Response;

    beforeEach(async () => {
        fetchHelper = createFetchHelper();
        fetchStub = fetchHelper.stubFetch();
        fetchStub.rejects(new Error('Network error'));

        // A single cache shared by both mounts - the same instance an application holds for the
        // lifetime of its <Arc> provider, and the reason a cached failure would outlive the component.
        const queryCache = new QueryInstanceCache();
        const wrapper = ({ children }: { children: React.ReactNode }) =>
            React.createElement(QueryInstanceCacheContext.Provider, { value: queryCache },
                React.createElement(ArcContext.Provider, { value: config }, children));

        const firstMount = renderHook(() => useQuery(FakeQuery), { wrapper });
        await waitFor(() => firstMount.result.current[0].hasExceptions.should.be.true);
        firstMount.unmount();

        fetchStub.resetBehavior();
        fetchStub.resolves(successfulResponse);

        const rendered: QueryResultWithState<unknown>[] = [];
        const secondMount = renderHook(() => {
            const tuple = useQuery(FakeQuery);
            rendered.push(tuple[0]);
            return tuple;
        }, { wrapper });

        firstRenderAfterRemount = rendered[0];

        await waitFor(() => secondMount.result.current[0].hasData.should.be.true);

        settledResultAfterRemount = secondMount.result.current[0];
    });

    afterEach(() => {
        fetchHelper.restore();
    });

    it('should not start the remount from the failed result', () => firstRenderAfterRemount.hasExceptions.should.be.false);

    it('should settle the remount on the fresh payload', () => (settledResultAfterRemount.data as FakeQueryResult[]).should.deep.equal([{ id: '1', name: 'first' }]));

    it('should perform the query again on remount', () => fetchStub.should.have.been.calledTwice);
});
