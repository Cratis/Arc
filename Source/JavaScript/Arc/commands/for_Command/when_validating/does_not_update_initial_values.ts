// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { a_command } from '../given/a_command';
import { given } from '../../../given';

describe("when validating does not update initial values", given(a_command, context => {
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
        context.command.someProperty = 'initial value';
        context.command.setInitialValues({ someProperty: 'initial value' });
        context.command.someProperty = 'changed value';
        context.command.propertyChanged('someProperty');

        context.fetchStub.resolves({
            status: 200,
            json: async () => responseData
        });

        await context.command.validate();
    });

    afterEach(() => {
        context.fetchStub.restore();
    });

    it("should still have changes after validation", () => context.command.hasChanges.should.be.true);
}));
