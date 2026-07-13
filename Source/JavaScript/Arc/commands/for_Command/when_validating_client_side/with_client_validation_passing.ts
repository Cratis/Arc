// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Command } from '../../Command';
import { CommandValidator } from '../../CommandValidator';
import { PropertyDescriptor } from '../../../reflection/PropertyDescriptor';
import '../../../validation/RuleBuilderExtensions';
import sinon from 'sinon';
import { CommandResult } from '../../CommandResult';
import { createFetchHelper } from '../../../helpers/fetchHelper';

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

    constructor() {
        super(Object, false);
    }

    get requestParameters(): string[] {
        return [];
    }

    get properties(): string[] {
        return ['email', 'age'];
    }
}

describe("when validating client side with client validation passing", () => {
    let command: TestCommand;
    let fetchStub: sinon.SinonStub;
    let fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };
    let result: CommandResult<object>;

    beforeEach(() => {
        command = new TestCommand();
        command.setOrigin('http://localhost');
        command.email = 'test@example.com';
        command.age = 25;

        fetchHelper = createFetchHelper();
        fetchStub = fetchHelper.stubFetch();

        result = command.validateClientSide();
    });

    afterEach(() => {
        fetchHelper.restore();
    });

    it("should not call server", () => fetchStub.called.should.be.false);
    it("should return valid result", () => result.isValid.should.be.true);
});
