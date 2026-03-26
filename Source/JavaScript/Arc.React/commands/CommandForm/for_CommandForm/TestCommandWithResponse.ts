// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Command, CommandValidator } from '@cratis/arc/commands';
import { PropertyDescriptor } from '@cratis/arc/reflection';

export interface TestCommandContent {
    name?: string;
    email?: string;
}

export interface TestCommandResponse {
    id: string;
    message: string;
}

class TestCommandWithResponseValidator extends CommandValidator<TestCommandWithResponse> {
    constructor() {
        super();
    }
}

export class TestCommandWithResponse extends Command<TestCommandContent, TestCommandResponse> {
    readonly route = '/api/test-command-with-response';
    readonly validation = new TestCommandWithResponseValidator();
    readonly propertyDescriptors: PropertyDescriptor[] = [
        new PropertyDescriptor('name', String, true),
        new PropertyDescriptor('email', String, true)
    ];

    name?: string;
    email?: string;

    get properties(): string[] {
        return ['name', 'email'];
    }

    get requestParameters(): string[] {
        return [];
    }

    constructor() {
        super(Object, false);
    }
}
