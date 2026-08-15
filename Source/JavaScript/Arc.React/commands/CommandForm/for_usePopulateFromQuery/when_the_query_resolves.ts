// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { renderHook, waitFor } from '@testing-library/react';
import sinon from 'sinon';
import { createFetchHelper } from '@cratis/arc/helpers/fetchHelper';
import { ArcContext, ArcConfiguration } from '../../../ArcContext';
import { usePopulateFromQuery } from '../usePopulateFromQuery';
import { FakePopulateQuery, FakePopulateQueryResult } from './FakePopulateQuery';

describe('when the query resolves', () => {
    let fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };

    beforeEach(() => {
        fetchHelper = createFetchHelper();
        fetchHelper.stubFetch().resolves({
            json: async () => ({
                data: { name: 'Jane Austen', email: 'jane@example.com' },
                isSuccess: true, isAuthorized: true, isValid: true, hasExceptions: false,
                validationResults: [], exceptionMessages: [], exceptionStackTrace: '',
                paging: { page: 0, size: 0, totalItems: 0, totalPages: 0 }
            })
        } as Response);
    });

    afterEach(() => {
        fetchHelper.restore();
    });

    const config: ArcConfiguration = {
        microservice: 'test-microservice',
        apiBasePath: '/api',
        origin: 'https://example.com'
    };

    const wrapper = ({ children }: { children: React.ReactNode }) =>
        React.createElement(ArcContext.Provider, { value: config }, children);

    it('should return the resolved data', async () => {
        const { result } = renderHook(() => usePopulateFromQuery<FakePopulateQueryResult, FakePopulateQuery>(FakePopulateQuery), { wrapper });
        await waitFor(() => (result.current !== undefined).should.be.true);
        result.current!.name.should.equal('Jane Austen');
        result.current!.email.should.equal('jane@example.com');
    });
});
