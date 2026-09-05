// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ValidationResultReason } from '../../validation/ValidationResultReason';
import { ValidationResultSeverity } from '../../validation/ValidationResultSeverity';
import { QueryResult } from '../QueryResult';

describe('when constructing from a server result with a constraint violation', () => {
    const result = new QueryResult({
        data: {},
        isSuccess: false,
        isAuthorized: true,
        isValid: false,
        hasExceptions: false,
        validationResults: [
            {
                severity: ValidationResultSeverity.Error,
                message: 'The organization number is already in use',
                members: ['organizationNumber'],
                state: {},
                reason: ValidationResultReason.ConstraintViolation,
                reasonDetail: 'UniqueOrganizationNumber'
            }
        ],
        exceptionMessages: [],
        exceptionStackTrace: '',
        paging: { page: 0, size: 0, totalItems: 0, totalPages: 0 }
    }, Object, false);

    it('should carry what kind of thing rejected the query', () =>
        result.validationResults[0].reason.should.equal(ValidationResultReason.ConstraintViolation));

    it('should carry the name of the violated constraint', () =>
        result.validationResults[0].reasonDetail!.should.equal('UniqueOrganizationNumber'));
});
