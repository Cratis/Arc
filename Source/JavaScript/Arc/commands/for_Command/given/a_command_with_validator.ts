// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Command } from '../../Command';
import { CommandValidator } from '../../CommandValidator';
import { PropertyDescriptor } from '../../../reflection/PropertyDescriptor';
import { createFetchHelper } from '../../../helpers/fetchHelper';
import sinon from 'sinon';
import '../../../validation/RuleBuilderExtensions';

interface ITestCommand {
    email: string;
    age: number;
}

class TestCommandValidator extends CommandValidator<ITestCommand> {
    constructor() {
        super();
        this.ruleFor(c => c.email).notEmpty().withMessage('Email is required');
        this.ruleFor(c => c.age).greaterThanOrEqual(18).withMessage('Must be at least 18 years old');
    }
}

class TestCommand extends Command<ITestCommand> {
    readonly route: string = '/api/test-command';
    readonly validation = new TestCommandValidator();
    readonly propertyDescriptors: PropertyDescriptor[] = [];
    
    email: string = '';
    age: number = 0;

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

export class a_command_with_validator {
    command: TestCommand;
    fetchStub: sinon.SinonStub;
    fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };

    constructor() {
        this.command = new TestCommand();
        this.fetchHelper = createFetchHelper();
        this.fetchStub = this.fetchHelper.stubFetch();
    }
}
