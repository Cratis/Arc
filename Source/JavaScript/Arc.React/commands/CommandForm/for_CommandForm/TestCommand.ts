// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Command, CommandValidator } from '@cratis/arc/commands';
import { PropertyDescriptor } from '@cratis/arc/reflection';

export interface TestCommandContent {
    name?: string;
    email?: string;
    age?: number;
}

class TestCommandValidator extends CommandValidator<TestCommand> {
    constructor() {
        super();
    }
}

export class TestCommand extends Command<TestCommandContent> {
    readonly route = '/api/test-command';
    readonly validation = new TestCommandValidator();
    readonly propertyDescriptors: PropertyDescriptor[] = [
        new PropertyDescriptor('name', String, true),
        new PropertyDescriptor('email', String, true),
        new PropertyDescriptor('age', Number, true)
    ];

    name?: string;
    email?: string;
    age?: number;

    get properties(): string[] {
        return ['name', 'email', 'age'];
    }

    get requestParameters(): string[] {
        return [];
    }

    constructor() {
        super(Object, false);
    }
}
