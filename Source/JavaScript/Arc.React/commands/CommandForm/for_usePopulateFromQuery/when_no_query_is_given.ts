// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { renderHook } from '@testing-library/react';
import sinon from 'sinon';
import { createFetchHelper } from '@cratis/arc/helpers/fetchHelper';
import { ArcContext, ArcConfiguration } from '../../../ArcContext';
import { usePopulateFromQuery } from '../usePopulateFromQuery';

describe('when no query is given', () => {
    let fetchStub: sinon.SinonStub;
    let fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };

    beforeEach(() => {
        fetchHelper = createFetchHelper();
        fetchStub = fetchHelper.stubFetch();
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

    it('should not fetch anything', async () => {
        renderHook(() => usePopulateFromQuery(undefined, undefined), { wrapper });
        await new Promise(resolve => setTimeout(resolve, 50));
        fetchStub.called.should.be.false;
    });

    it('should return undefined', () => {
        const { result } = renderHook(() => usePopulateFromQuery(undefined, undefined), { wrapper });
        (result.current === undefined).should.be.true;
    });
});
