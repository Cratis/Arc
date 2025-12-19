// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { createFetchHelper } from '../../../helpers/fetchHelper';
import { SomeCommand } from '../SomeCommand';
import { given } from '../../../given';

describe("when executing with origin and api base path", given(class {
    command: SomeCommand;
    fetchStub: sinon.SinonStub;
    fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };

    constructor() {
        this.command = new SomeCommand();
        this.command.route = '/items';
        this.command.setOrigin('https://api.example.com');
        this.command.setApiBasePath('/api/v1');
        this.command.someProperty = 'test-value';
        this.fetchHelper = createFetchHelper();
        this.fetchStub = this.fetchHelper.stubFetch();
    }
}, context => {
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

        await context.command.execute();
    });

    afterEach(() => {
        context.fetchHelper.restore();
    });

    it("should_construct_url_with_origin_and_base_path", () => {
        const call = context.fetchStub.getCall(0);
        const url = call.args[0];
        url.toString().should.contain('https://api.example.com');
        url.toString().should.contain('/api/v1/items');
    });
}));
