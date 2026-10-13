// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { an_observable_query_for } from '../given/an_observable_query_for.js';
import { given } from '../../../given.js';
import { createFetchHelper } from '../../../helpers/fetchHelper.js';
import { QueryResult } from '../../QueryResult.js';
import { ValidationResult } from '../../../validation/ValidationResult.js';

describe('with server validation failing', given(an_observable_query_for, context => {
    let result: QueryResult<string>;
    let fetchHelper: ReturnType<typeof createFetchHelper>;

    beforeEach(async () => {
        fetchHelper = createFetchHelper();
        fetchHelper.stubFetch().resolves({
            ok: false,
            status: 400,
            json: sinon.stub().resolves({
                data: null,
                isSuccess: false,
                isAuthorized: true,
                isValid: false,
                hasExceptions: false,
                validationResults: [{ severity: 2, message: 'Id is invalid.', members: ['id'], state: {} }],
                exceptionMessages: [],
                exceptionStackTrace: '',
                paging: { page: 0, size: 0, totalItems: 0, totalPages: 0 }
            })
        } as unknown as Response);
        context.query.setOrigin('https://example.com');
        result = await context.query.perform({ id: 'invalid' });
    });

    afterEach(() => fetchHelper.restore());

    it('should be invalid', () => result.isValid.should.be.false);
    it('should not be successful', () => result.isSuccess.should.be.false);
    it('should map the validation result', () => result.validationResults[0].should.be.instanceOf(ValidationResult));
    it('should preserve the message', () => result.validationResults[0].message.should.equal('Id is invalid.'));
    it('should preserve the members', () => result.validationResults[0].members.should.deep.equal(['id']));
}));
