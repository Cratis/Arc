// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ValidationResultReason } from '../../validation/ValidationResultReason';
import { ValidationResultSeverity } from '../../validation/ValidationResultSeverity';
import { CommandResult } from '../CommandResult';

describe('when constructing from a server result with a constraint violation', () => {
    const result = new CommandResult({
        correlationId: '0c0ee8c8-b5a6-4999-b030-6e6a0c931b91',
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
            },
            {
                severity: ValidationResultSeverity.Error,
                message: 'Name is required',
                members: ['name'],
                state: {}
            }
        ],
        exceptionMessages: [],
        exceptionStackTrace: '',
        authorizationFailureReason: '',
        response: null
    }, String, false);

    it('should carry the name of the violated constraint across the wire', () =>
        result.validationResults[0].reasonDetail!.should.equal('UniqueOrganizationNumber'));

    // A reason with no finer identity - and a server that predates this - both send nothing.
    it('should leave the detail undefined when the server sends none', () =>
        (result.validationResults[1].reasonDetail === undefined).should.be.true);

    it('should let a client tell one constraint from another without reading the message', () =>
        result.validationResults
            .filter(_ => _.reason === ValidationResultReason.ConstraintViolation && _.reasonDetail === 'UniqueOrganizationNumber')
            .length.should.equal(1));
});
