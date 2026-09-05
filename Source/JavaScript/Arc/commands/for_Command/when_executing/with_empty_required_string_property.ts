// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from '../../../given';
import { CommandResult } from '../../CommandResult';
import { a_command } from '../given/a_command';

describe("when executing with empty required string property", given(class extends a_command {
    constructor() {
        super();
        this.command.someProperty = '';
    }
}, context => {
    let result: CommandResult<object>;
    const responseData = {
        correlationId: '12345678-1234-1234-1234-123456789012',
        isSuccess: true,
        isAuthorized: true,
        isValid: true,
        hasExceptions: false,
        validationResults: [],
        exceptionMessages: [],
        exceptionStackTrace: '',
        response: {}
    };

    beforeEach(async () => {
        context.fetchStub.resolves({
            status: 200,
            json: async () => responseData
        });

        result = await context.command.execute();
    });

    afterEach(() => {
        context.fetchStub.restore();
    });

    it("should call fetch", () => context.fetchStub.calledOnce.should.be.true);
    it("should return valid result", () => result.isValid.should.be.true);
    it("should send empty string value", () => {
        const call = context.fetchStub.getCall(0);
        const body = JSON.parse(call.args[1].body);
        body.someProperty.should.equal('');
    });
}));
