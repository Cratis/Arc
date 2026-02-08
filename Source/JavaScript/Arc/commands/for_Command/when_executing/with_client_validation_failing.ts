// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Command } from '../../Command';
import { CommandValidator } from '../../CommandValidator';
import { CommandResult } from '../../CommandResult';
import { PropertyDescriptor } from '../../../reflection/PropertyDescriptor';
import '../../../validation/RuleBuilderExtensions';

class TestCommandValidator extends CommandValidator<{ name: string; age: number }> {
    constructor() {
        super();
        this.ruleFor(c => c.name).notEmpty();
        this.ruleFor(c => c.age).greaterThanOrEqual(18);
    }
}

class TestCommand extends Command<{ name: string; age: number }, void> {
    readonly route = '/test';
    readonly validation = new TestCommandValidator();
    readonly propertyDescriptors: PropertyDescriptor[] = [];
    name = '';
    age = 0;

    constructor() {
        super(Object, false);
    }

    get requestParameters(): string[] {
        return [];
    }

    get properties(): string[] {
        return ['name', 'age'];
    }
}

describe("when executing with client validation failing", () => {
    let command: TestCommand;
    let result: CommandResult<void>;

    beforeEach(async () => {
        command = new TestCommand();
        command.name = '';
        command.age = 15;

        result = await command.execute();
    });

    it("should not be success", () => {
        result.isSuccess.should.be.false;
    });

    it("should not be valid", () => {
        result.isValid.should.be.false;
    });

    it("should have validation results", () => {
        result.validationResults.should.not.be.empty;
    });

    it("should have error for name", () => {
        result.validationResults.some(r => r.members.includes('name')).should.be.true;
    });

    it("should have error for age", () => {
        result.validationResults.some(r => r.members.includes('age')).should.be.true;
    });
});
