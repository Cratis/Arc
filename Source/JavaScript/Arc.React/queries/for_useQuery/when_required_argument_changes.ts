// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { renderHook, waitFor } from '@testing-library/react';
import sinon from 'sinon';
import { useQuery } from '../useQuery';
import { FakeQueryWithRequiredParameters, FakeQueryWithRequiredParametersArguments } from './FakeQueryWithRequiredParameters';
import { ArcContext, ArcConfiguration } from '../../ArcContext';
import { createFetchHelper } from '@cratis/arc/helpers/fetchHelper';

describe('when required argument changes', () => {
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

    it('should execute query again when argument changes', async () => {
        const { rerender } = renderHook(
            ({ args }: { args: FakeQueryWithRequiredParametersArguments }) => useQuery(FakeQueryWithRequiredParameters, args),
            {
                wrapper,
                initialProps: { args: { userId: 'user-1', category: 'cat-1' } }
            }
        );

        await waitFor(() => fetchStub.should.have.been.calledOnce);

        fetchStub.resetHistory();

        rerender({ args: { userId: 'user-2', category: 'cat-1' } });

        await waitFor(() => fetchStub.should.have.been.calledOnce);
    });
});
