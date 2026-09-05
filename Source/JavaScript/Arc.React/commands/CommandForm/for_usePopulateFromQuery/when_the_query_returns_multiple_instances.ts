// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { renderHook } from '@testing-library/react';
import { ArcContext, ArcConfiguration } from '../../../ArcContext';
import { usePopulateFromQuery } from '../usePopulateFromQuery';
import { FakeEnumerablePopulateQuery, FakePopulateQueryResult } from './FakePopulateQuery';

describe('when the query returns multiple instances', () => {
    const config: ArcConfiguration = {
        microservice: 'test-microservice',
        apiBasePath: '/api',
        origin: 'https://example.com'
    };

    const wrapper = ({ children }: { children: React.ReactNode }) =>
        React.createElement(ArcContext.Provider, { value: config }, children);

    it('should throw', () => {
        (() => renderHook(
            () => usePopulateFromQuery<FakePopulateQueryResult[], FakeEnumerablePopulateQuery>(FakeEnumerablePopulateQuery),
            { wrapper })
        ).should.throw('returns multiple instances');
    });
});
