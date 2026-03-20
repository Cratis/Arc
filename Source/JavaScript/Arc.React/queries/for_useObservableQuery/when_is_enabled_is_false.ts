// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { renderHook } from '@testing-library/react';
import { useObservableQuery } from '../useObservableQuery';
import { FakeObservableQuery } from './FakeObservableQuery';
import { ArcContext, ArcConfiguration } from '../../ArcContext';

/* eslint-disable @typescript-eslint/no-explicit-any */

describe('when is_enabled is false', () => {
    beforeEach(() => {
        FakeObservableQuery.reset();
    });

    const config: ArcConfiguration = {
        microservice: 'test-microservice',
        apiBasePath: '/api',
        origin: 'https://example.com',
    };

    const wrapper = ({ children }: { children: React.ReactNode }) =>
        React.createElement(ArcContext.Provider, { value: config }, children);

    it('should not call subscribe on the query', () => {
        renderHook(() => useObservableQuery(FakeObservableQuery, undefined, undefined, false), { wrapper });
        FakeObservableQuery.subscribeCallbacks.length.should.equal(0);
    });

    it('should return an empty result', () => {
        const { result } = renderHook(() => useObservableQuery(FakeObservableQuery, undefined, undefined, false), { wrapper });
        result.current[0].hasData.should.be.false;
    });
});
