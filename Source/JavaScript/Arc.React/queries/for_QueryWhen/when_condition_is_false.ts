// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { renderHook, waitFor } from '@testing-library/react';
import sinon from 'sinon';
import { QueryWhen } from '../QueryWhen';
import { FakeQuery } from '../for_useQuery/FakeQuery';
import { ArcContext, ArcConfiguration } from '../../ArcContext';
import { createFetchHelper } from '@cratis/arc/helpers/fetchHelper';

/* eslint-disable @typescript-eslint/no-explicit-any */

describe('when condition is false', () => {
    let fetchStub: sinon.SinonStub;
    let fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };
    let queryWhen: QueryWhen<FakeQuery, unknown[]>;

    beforeEach(() => {
        fetchHelper = createFetchHelper();
        fetchStub = fetchHelper.stubFetch();
        fetchStub.resolves({
            json: async () => ({ data: [], isSuccess: true, isAuthorized: true, isValid: true, hasExceptions: false, validationResults: [], exceptionMessages: [], exceptionStackTrace: '' })
        } as Response);

        queryWhen = new QueryWhen<FakeQuery, unknown[]>(FakeQuery, false);
    });

    afterEach(() => {
        fetchHelper.restore();
    });

    const config: ArcConfiguration = {
        microservice: 'test-microservice',
        apiBasePath: '/api',
        origin: 'https://example.com',
    };

    const wrapper = ({ children }: { children: React.ReactNode }) =>
        React.createElement(ArcContext.Provider, { value: config }, children);

    it('should not fetch when use() is called', async () => {
        renderHook(() => queryWhen.use(), { wrapper });
        await new Promise(resolve => setTimeout(resolve, 50));
        fetchStub.called.should.be.false;
    });

    it('should return an empty result from use()', () => {
        const { result } = renderHook(() => queryWhen.use(), { wrapper });
        result.current[0].hasData.should.be.false;
    });
});
