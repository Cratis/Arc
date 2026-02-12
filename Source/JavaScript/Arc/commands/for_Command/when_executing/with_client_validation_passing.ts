// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Command } from '../../Command';
import { CommandValidator } from '../../CommandValidator';
import { PropertyDescriptor } from '../../../reflection/PropertyDescriptor';
import '../../../validation/RuleBuilderExtensions';
import sinon from 'sinon';
import { createFetchHelper } from '../../../helpers/fetchHelper';

class TestCommandValidator extends CommandValidator<{ name: string }> {
    constructor() {
        super();
        this.ruleFor(c => c.name).notEmpty();
    }
}

class TestCommand extends Command<{ name: string }, void> {
    readonly route = '/test';
    readonly validation = new TestCommandValidator();
    readonly propertyDescriptors: PropertyDescriptor[] = [];
    name = '';

    constructor() {
        super(Object, false);
    }

    get requestParameters(): string[] {
        return [];
    }

    get properties(): string[] {
        return ['name'];
    }
}

describe("when executing with client validation passing", () => {
    let command: TestCommand;
    let fetchStub: sinon.SinonStub;
    let fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };

    beforeEach(() => {
        command = new TestCommand();
        command.setOrigin('http://localhost');
        command.name = 'test';

        fetchHelper = createFetchHelper();
        fetchStub = fetchHelper.stubFetch();
        fetchStub.resolves({
            ok: true,
            json: async () => ({ isSuccess: true, isValid: true })
        } as Response);
    });

    afterEach(() => {
        fetchHelper.restore();
    });

    it("should call server", async () => {
        await command.execute();
        fetchStub.should.have.been.called;
    });
});
