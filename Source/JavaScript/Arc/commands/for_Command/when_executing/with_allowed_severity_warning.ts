// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from '../../../given';
import { a_command } from '../given/a_command';
import { ValidationResultSeverity } from '../../../validation/ValidationResultSeverity';
import { CommandResult } from '../../CommandResult';

describe("when executing with allowed severity warning", given(class extends a_command {
    constructor() {
        super();
        
        // Setup fetch to respond normally - we're just testing that the header gets sent
        this.fetchStub.resolves({
            status: 200,
            json: async () => ({
                correlationId: '00000000-0000-0000-0000-000000000000',
                isSuccess: true,
                isAuthorized: true,
                isValid: true,
                hasExceptions: false,
                validationResults: [],
                exceptionMessages: [],
                exceptionStackTrace: '',
                authorizationFailureReason: '',
                response: null
            })
        });
    }
}, context => {
    beforeEach(async () => {
        await context.command.execute(ValidationResultSeverity.Warning);
    });

    afterEach(() => {
        context.fetchHelper.restore();
    });

    it("should call fetch", () => context.fetchStub.called.should.be.true);
    it("should have sent X-Allowed-Severity header with value 2", () => {
        const headers = context.fetchStub.firstCall.args[1].headers;
        headers['X-Allowed-Severity'].should.equal('2'); // Warning = 2
    });
}));
