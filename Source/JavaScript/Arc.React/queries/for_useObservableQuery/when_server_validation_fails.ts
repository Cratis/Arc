// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { act, renderHook } from '@testing-library/react';
import { QueryInstanceCache, QueryResult, QueryResultWithState } from '@cratis/arc/queries';
import { useObservableQuery } from '../useObservableQuery.js';
import { FakeObservableQuery } from './FakeObservableQuery.js';
import { QueryInstanceCacheContext } from '../QueryInstanceCacheContext.js';

describe('when server validation fails', () => {
    let queryResult: QueryResultWithState<unknown>;

    beforeEach(async () => {
        FakeObservableQuery.reset();
        const cache = new QueryInstanceCache();
        const wrapper = ({ children }: { children: React.ReactNode }) =>
            React.createElement(QueryInstanceCacheContext.Provider, { value: cache }, children);
        const { result } = renderHook(() => useObservableQuery(FakeObservableQuery), { wrapper });
        await act(async () => {
            FakeObservableQuery.subscribeCallbacks[0](new QueryResult({
                data: null,
                isSuccess: false,
                isAuthorized: true,
                isValid: false,
                hasExceptions: false,
                validationResults: [{ severity: 2, message: 'Search term is required.', members: ['searchTerm'], state: {} }],
                exceptionMessages: [],
                exceptionStackTrace: '',
                paging: { page: 0, size: 0, totalItems: 0, totalPages: 0 }
            }, Object, true));
        });
        queryResult = result.current[0];
    });

    it('should be invalid', () => queryResult.isValid.should.be.false);
    it('should not be successful', () => queryResult.isSuccess.should.be.false);
    it('should stop performing', () => queryResult.isPerforming.should.be.false);
    it('should preserve the message', () => queryResult.validationResults[0].message.should.equal('Search term is required.'));
    it('should preserve the members', () => queryResult.validationResults[0].members.should.deep.equal(['searchTerm']));
});
