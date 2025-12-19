// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { SomeCommand } from '../SomeCommand';
import { createFetchHelper } from '../../../helpers/fetchHelper';
import { given } from '../../../given';

describe("when executing with custom http headers", given(class {
    command: SomeCommand;
    fetchStub: sinon.SinonStub;
    fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };

    constructor() {
        this.command = new SomeCommand();
        this.command.route = '/test-route';
        this.command.setOrigin('http://localhost');
        this.command.setApiBasePath('/api');
        this.command.someProperty = 'test-value';
        this.command.setHttpHeadersCallback(() => ({
            'X-Custom-Header': 'custom-value',
            'Authorization': 'Bearer token123'
        }));
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
        context.fetchStub.restore();
    });

    it("should_include_custom_headers", () => {
        const call = context.fetchStub.getCall(0);
        call.args[1].headers['X-Custom-Header'].should.equal('custom-value');
        call.args[1].headers['Authorization'].should.equal('Bearer token123');
    });

    it("should_include_default_headers", () => {
        const call = context.fetchStub.getCall(0);
        call.args[1].headers['Content-Type'].should.equal('application/json');
        call.args[1].headers['Accept'].should.equal('application/json');
    });
}));
