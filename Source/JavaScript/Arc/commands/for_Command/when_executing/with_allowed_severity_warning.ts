// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from '../../../given';
import { a_command } from '../given/a_command';
import { ValidationResultSeverity } from '../../../validation/ValidationResultSeverity';
import { CommandResult } from '../../CommandResult';

describe("when executing with allowed severity warning", given(class extends a_command {
    constructor() {
        super();
        
        // Setup fetch to respond with a warning validation result
        this.fetchStub.resolves({
            ok: true,
            json: async () => ({
                correlationId: '00000000-0000-0000-0000-000000000000',
                isSuccess: false,
                isAuthorized: true,
                isValid: false,
                hasExceptions: false,
                validationResults: [{
                    severity: ValidationResultSeverity.Warning,
                    message: 'This is a warning from server',
                    members: ['someProperty'],
                    state: null
                }],
                exceptionMessages: [],
                exceptionStackTrace: '',
                authorizationFailureReason: '',
                response: null
            })
        } as Response);
    }
}, context => {
    let result: CommandResult<object>;

    beforeEach(async () => {
        result = await context.command.execute(ValidationResultSeverity.Warning);
    });

    afterEach(() => {
        context.fetchHelper.restore();
    });

    it("should call fetch", () => context.fetchStub.called.should.be.true);
    it("should have sent X-Allowed-Severity header", () => {
        const headers = context.fetchStub.firstCall.args[1].headers;
        headers['X-Allowed-Severity'].should.equal('2'); // Warning = 2
    });
}));
