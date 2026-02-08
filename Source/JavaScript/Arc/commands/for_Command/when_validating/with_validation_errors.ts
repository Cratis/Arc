// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { a_command } from '../given/a_command';
import { given } from '../../../given';
import { CommandResult } from '../../CommandResult';

describe("when validating with validation errors", given(a_command, context => {
    let result: CommandResult<object>;
    const responseData = {
        correlationId: '12345678-1234-1234-1234-123456789012',
        isSuccess: false,
        isAuthorized: true,
        isValid: false,
        hasExceptions: false,
        validationResults: [
            {
                severity: 1,
                message: 'Field is required',
                members: ['someProperty'],
                state: {}
            }
        ],
        exceptionMessages: [],
        exceptionStackTrace: '',
        response: {}
    };

    beforeEach(async () => {
        context.fetchStub.resolves({
            status: 200,
            json: async () => responseData
        });

        result = await context.command.validate();
    });

    afterEach(() => {
        context.fetchStub.restore();
    });

    it("should return invalid result", () => result.isValid.should.be.false);
    it("should not be successful", () => result.isSuccess.should.be.false);
    it("should not update initial values", () => context.command.hasChanges.should.be.false);
}));
