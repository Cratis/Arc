// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render } from '@testing-library/react';
import sinon from 'sinon';
import { useQuery } from '../useQuery';
import { FakeQuery } from './FakeQuery';
import { ArcContext, ArcConfiguration } from '../../ArcContext';
import { createFetchHelper } from '@cratis/arc/helpers/fetchHelper';

/* eslint-disable @typescript-eslint/no-explicit-any */

describe('when creating instance without optional context values', () => {
    let fetchStub: sinon.SinonStub;
    let fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };
    let queryInstance: FakeQuery | null = null;

    const captureInstance = (instance: FakeQuery) => {
        queryInstance = instance;
    };

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
        microservice: 'test-microservice'
    };

    class SpyQuery extends FakeQuery {
        constructor() {
            super();
            captureInstance(this);
        }
    }

    render(
        React.createElement(
            ArcContext.Provider,
            { value: config },
            React.createElement(() => {
                useQuery(SpyQuery);
                return React.createElement('div', null, 'Test');
            })
        )
    );

    it('should set api base path to empty string', () => ((queryInstance as any)._apiBasePath).should.equal(''));
    it('should set origin to empty string', () => ((queryInstance as any)._origin).should.equal(''));
    it('should set http headers callback to return empty object', () => {
        const headers = (queryInstance as any)._httpHeadersCallback();
        headers.should.deep.equal({});
    });
});
