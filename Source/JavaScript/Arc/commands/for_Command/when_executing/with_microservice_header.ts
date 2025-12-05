// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from '../../../given';
import { Globals } from '../../../Globals';
import { a_command } from '../given/a_command';

describe("when executing with microservice header", given(class extends a_command {
    originalMicroserviceHeader: string;

    constructor() {
        super();
        this.command.setMicroservice('my-microservice');
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
