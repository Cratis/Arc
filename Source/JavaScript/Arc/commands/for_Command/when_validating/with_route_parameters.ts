// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { createFetchHelper } from '../../../helpers/fetchHelper';
import { CommandWithRouteParams } from '../CommandWithRouteParams';
import { given } from '../../../given';

describe("when validating with route parameters", given(class {
    command: CommandWithRouteParams;
    fetchStub: sinon.SinonStub;
    fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };

    constructor() {
        this.command = new CommandWithRouteParams();
        this.command.setOrigin('http://localhost');
        this.command.setApiBasePath('/api');
        this.command.id = '123';
        this.command.name = 'Test Item';
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

        await context.command.validate();
    });

    afterEach(() => {
        context.fetchHelper.restore();
    });

    it("should replace route parameters in url and append validate", () => {
        const call = context.fetchStub.getCall(0);
        const url = call.args[0];
        url.toString().should.contain('/api/items/123/validate');
    });

    it("should include all properties in body", () => {
        const call = context.fetchStub.getCall(0);
        const body = JSON.parse(call.args[1].body);
        body.id.should.equal('123');
        body.name.should.equal('Test Item');
    });
}));
