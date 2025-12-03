// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { SomeCommand } from '../SomeCommand';
import { given } from '../../../given';
import { Globals } from '../../../Globals';

describe("when executing with microservice header", given(class {
    command: SomeCommand;
    fetchStub: sinon.SinonStub;
    originalMicroserviceHeader: string;

    constructor() {
        this.command = new SomeCommand();
        this.command.route = '/test-route';
        this.command.setOrigin('http://localhost');
        this.command.setApiBasePath('/api');
        this.command.someProperty = 'test-value';
        this.command.setMicroservice('my-microservice');
        this.fetchStub = sinon.stub(globalThis, 'fetch');
        this.originalMicroserviceHeader = Globals.microserviceHttpHeader;
        Globals.microserviceHttpHeader = 'X-Microservice-Id';
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
        Globals.microserviceHttpHeader = context.originalMicroserviceHeader;
    });

    it("should_include_microservice_header", () => {
        const call = context.fetchStub.getCall(0);
        call.args[1].headers['X-Microservice-Id'].should.equal('my-microservice');
    });
}));
