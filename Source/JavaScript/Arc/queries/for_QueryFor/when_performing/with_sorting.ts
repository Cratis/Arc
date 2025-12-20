// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { a_query_for } from '../given/a_query_for';
import { given } from '../../../given';

import * as sinon from 'sinon';
import { createFetchHelper } from '../../../helpers/fetchHelper';
import { QueryResult } from '../../QueryResult';
import { Sorting } from '../../Sorting';
import { SortDirection } from '../../SortDirection';

describe('with sorting', given(a_query_for, context => {
    let result: QueryResult<string>;
    let fetchStub: sinon.SinonStub;
    let fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };
                const mockResponse = {
                data: 'test-result',
                isSuccess: true,
                isAuthorized: true,
                isValid: true,
                hasExceptions: false,
                validationResults: [],
                exceptionMessages: [],
                exceptionStackTrace: '',
                paging: {
                    totalItems: 0,
                    totalPages: 0,
                    page: 0,
                    size: 0
                }
            };

    describe('and ascending direction', () => {
        beforeEach(async () => {
            // Setup fetch mock
            fetchHelper = createFetchHelper();
            fetchStub = fetchHelper.stubFetch();
            fetchStub.resolves({
                json: sinon.stub().resolves(mockResponse),
                ok: true,
                status: 200
            } as unknown as Response);

            context.query.setOrigin('https://api.example.com');
            context.query.sorting = new Sorting('name', SortDirection.ascending);

            // Call perform with valid arguments
            result = await context.query.perform({ id: 'test-id' });
        });

        afterEach(() => {
            fetchHelper.restore();
        });

        it('should call fetch with URL including sorting parameters', () => {
            fetchStub.should.have.been.calledOnce;
            const call = fetchStub.getCall(0);
            const url = call.args[0].href;
            url.should.include('sortBy=name');
            url.should.include('sortDirection=asc');
        });

        it('should return successful result', () => {
            result.isSuccess.should.be.true;
        });
    });

    describe('and descending direction', () => {
        beforeEach(async () => {
            // Setup fetch mock
            fetchHelper = createFetchHelper();
            fetchStub = fetchHelper.stubFetch();
            fetchStub.resolves({
                json: sinon.stub().resolves(mockResponse),
                ok: true,
                status: 200
            } as unknown as Response);

            context.query.setOrigin('https://api.example.com');
            context.query.sorting = new Sorting('name', SortDirection.descending);

            // Call perform with valid arguments
            result = await context.query.perform({ id: 'test-id' });
        });

        afterEach(() => {
            fetchHelper.restore();
        });

        it('should call fetch with URL including sorting parameters', () => {
            fetchStub.should.have.been.calledOnce;
            const call = fetchStub.getCall(0);
            const url = call.args[0].href;
            url.should.include('sortBy=name');
            url.should.include('sortDirection=desc');
        });

        it('should return successful result', () => {
            result.isSuccess.should.be.true;
        });
    });
}));