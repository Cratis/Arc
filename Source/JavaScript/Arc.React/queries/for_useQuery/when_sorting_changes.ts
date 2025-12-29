// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { renderHook, waitFor } from '@testing-library/react';
import sinon from 'sinon';
import { useQuery } from '../useQuery';
import { FakeQuery } from './FakeQuery';
import { ArcContext, ArcConfiguration } from '../../ArcContext';
import { createFetchHelper } from '@cratis/arc/helpers/fetchHelper';
import { Sorting, SortDirection } from '@cratis/arc/queries';

describe('when sorting changes', () => {
    let fetchStub: sinon.SinonStub;
    let fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };

    beforeEach(() => {
        fetchHelper = createFetchHelper();
        fetchStub = fetchHelper.stubFetch();
        fetchStub.resolves({
            json: async () => ({ data: [], isSuccess: true, isAuthorized: true, isValid: true, hasExceptions: false, validationResults: [], exceptionMessages: [], exceptionStackTrace: '' })
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

    const wrapper = ({ children }: { children: React.ReactNode }) => (
        React.createElement(ArcContext.Provider, { value: config }, children)
    );

    it('should execute query again when sorting changes', async () => {
        const { result } = renderHook(
            () => useQuery(FakeQuery),
            { wrapper }
        );

        await waitFor(() => fetchStub.should.have.been.calledOnce);

        fetchStub.resetHistory();

        const [, , setSorting] = result.current;
        await setSorting(new Sorting('name', SortDirection.ascending));

        await waitFor(() => fetchStub.should.have.been.calledOnce);
    });
});
