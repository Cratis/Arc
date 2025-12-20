// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Command } from '../../Command';
import { CommandValidator } from '../../CommandValidator';
import { PropertyDescriptor } from '../../../reflection/PropertyDescriptor';
import '../../../validation/RuleBuilderExtensions';
import sinon from 'sinon';
import { CommandResult } from '../../CommandResult';

interface ITestCommand {
    email: string;
    age: number;
}

class TestCommandValidator extends CommandValidator<ITestCommand> {
    constructor() {
        super();
        this.ruleFor(c => c.email).notEmpty().emailAddress();
        this.ruleFor(c => c.age).greaterThanOrEqual(18);
    }
}

class TestCommand extends Command<ITestCommand> {
    readonly route = '/api/test';
    readonly validation = new TestCommandValidator();
    readonly propertyDescriptors: PropertyDescriptor[] = [];
    email = '';
    age = 0;

    get requestParameters(): string[] {
        return [];
    }

    get properties(): string[] {
        return ['email', 'age'];
    }
}

describe("when validating with client validation passing", () => {
    let command: TestCommand;
    let fetchStub: sinon.SinonStub;
    let result: CommandResult<object>;

    beforeEach(async () => {
        command = new TestCommand();
        command.setOrigin('http://localhost');
        command.email = 'test@example.com';
        command.age = 25;

        fetchStub = sinon.stub(global, 'fetch').resolves({
            ok: true,
            json: async () => ({ isSuccess: true, isValid: true, validationResults: [] })
        } as Response);

        result = await command.validate();
    });

    afterEach(() => {
        fetchStub.restore();
    });

    it("should_call_server", () => fetchStub.calledOnce.should.be.true);
    it("should_call_validation_endpoint", () => {
        const url = fetchStub.getCall(0).args[0];
        url.toString().should.contain('/api/test/validate');
    });
    it("should_return_valid_result", () => result.isValid.should.be.true);
});
