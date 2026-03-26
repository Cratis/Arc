// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { renderHook } from '@testing-library/react';
import { ObservableQueryWhen } from '../ObservableQueryWhen';
import { FakeObservableQuery } from '../for_useObservableQuery/FakeObservableQuery';
import { ArcContext, ArcConfiguration } from '../../ArcContext';

/* eslint-disable @typescript-eslint/no-explicit-any */

describe('when observable condition is false', () => {
    let subscribeCalledBefore: number;

    beforeEach(() => {
        FakeObservableQuery.reset();
        subscribeCalledBefore = FakeObservableQuery.subscribeCallbacks.length;
    });

    const config: ArcConfiguration = {
        microservice: 'test-microservice',
        apiBasePath: '/api',
        origin: 'https://example.com',
    };

    const wrapper = ({ children }: { children: React.ReactNode }) =>
        React.createElement(ArcContext.Provider, { value: config }, children);

    it('should not subscribe when use() is called', () => {
        const queryWhen = new ObservableQueryWhen<FakeObservableQuery, unknown[]>(FakeObservableQuery, false);
        renderHook(() => queryWhen.use(), { wrapper });
        FakeObservableQuery.subscribeCallbacks.length.should.equal(subscribeCalledBefore);
    });

    it('should return an empty result from use()', () => {
        const queryWhen = new ObservableQueryWhen<FakeObservableQuery, unknown[]>(FakeObservableQuery, false);
        const { result } = renderHook(() => queryWhen.use(), { wrapper });
        result.current[0].hasData.should.be.false;
    });
});
