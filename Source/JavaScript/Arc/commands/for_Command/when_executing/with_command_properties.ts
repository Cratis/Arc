// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { a_command } from '../given/a_command';
import { given } from '../../../given';

describe("when executing with command properties", given(a_command, context => {
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
        context.command.someProperty = 'test value';

        context.fetchStub.resolves({
            status: 200,
            json: async () => responseData
        });

        await context.command.execute();
    });

    afterEach(() => {
        context.fetchStub.restore();
    });

    it("should include command properties in body", () => {
        const call = context.fetchStub.getCall(0);
        const body = JSON.parse(call.args[1].body);
        body.someProperty.should.equal('test value');
    });
}));
