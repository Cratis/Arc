// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render } from '@testing-library/react';
import { useQuery } from '../useQuery';
import { FakeQuery } from './FakeQuery';
import { ArcContext, ArcConfiguration } from '../../ArcContext';
import { Sorting, SortDirection } from '@cratis/arc/queries';
import { createFetchHelper } from '@cratis/arc/helpers/fetchHelper';

describe('when creating instance with sorting', () => {
    let fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };
    let queryInstance: FakeQuery | null = null;

    const captureInstance = (instance: FakeQuery) => {
        queryInstance = instance;
    };

    beforeEach(() => {
        fetchHelper = createFetchHelper();
        const fetchStub = fetchHelper.stubFetch();
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

    const sorting = new Sorting('name', 1);
    
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
                useQuery(SpyQuery, undefined, sorting);
                return React.createElement('div', null, 'Test');
            })
        )
    );

    it('should set sorting on the query', () => queryInstance!.sorting.should.equal(sorting));
});
