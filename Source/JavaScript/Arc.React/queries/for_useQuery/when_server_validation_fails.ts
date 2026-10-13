// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { renderHook, waitFor } from '@testing-library/react';
import sinon from 'sinon';
import { QueryInstanceCache, QueryResultWithState } from '@cratis/arc/queries';
import { createFetchHelper } from '@cratis/arc/helpers/fetchHelper';
import { useQuery } from '../useQuery.js';
import { FakeQuery } from './FakeQuery.js';
import { ArcContext } from '../../ArcContext.js';
import { QueryInstanceCacheContext } from '../QueryInstanceCacheContext.js';

describe('when server validation fails', () => {
    let fetchHelper: ReturnType<typeof createFetchHelper>;
    let queryResult: QueryResultWithState<unknown>;

    beforeEach(async () => {
        fetchHelper = createFetchHelper();
        fetchHelper.stubFetch().resolves({
            ok: false,
            status: 400,
            json: sinon.stub().resolves({
                data: null,
                isSuccess: false,
                isAuthorized: true,
                isValid: false,
                hasExceptions: false,
                validationResults: [{ severity: 2, message: 'Search term is required.', members: ['searchTerm'], state: {} }],
                exceptionMessages: [],
                exceptionStackTrace: '',
                paging: { page: 0, size: 0, totalItems: 0, totalPages: 0 }
            })
        } as unknown as Response);
        const cache = new QueryInstanceCache();
        const wrapper = ({ children }: { children: React.ReactNode }) =>
            React.createElement(QueryInstanceCacheContext.Provider, { value: cache },
                React.createElement(ArcContext.Provider, { value: { microservice: '', origin: 'https://example.com' } }, children));
        const { result } = renderHook(() => useQuery(FakeQuery), { wrapper });
        const initialResult = result.current[0];
        await waitFor(() => (result.current[0] !== initialResult).should.be.true);
        queryResult = result.current[0];
    });

    afterEach(() => fetchHelper.restore());

    it('should be invalid', () => queryResult.isValid.should.be.false);
    it('should not be successful', () => queryResult.isSuccess.should.be.false);
    it('should stop performing', () => queryResult.isPerforming.should.be.false);
    it('should preserve the message', () => queryResult.validationResults[0].message.should.equal('Search term is required.'));
    it('should preserve the members', () => queryResult.validationResults[0].members.should.deep.equal(['searchTerm']));
});
