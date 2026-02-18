// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Command, CommandValidator } from '@cratis/arc/commands';
import { PropertyDescriptor } from '@cratis/arc/reflection';

export interface TestCommandContent {
    requiredField: string;
    optionalField?: string;
}

export class TestCommand extends Command<TestCommandContent> {
    readonly route = '/api/test-command';
    readonly validation = {} as CommandValidator;
    readonly propertyDescriptors: PropertyDescriptor[] = [
        new PropertyDescriptor('requiredField', String, false),
        new PropertyDescriptor('optionalField', String, true)
    ];

    requiredField = '';
    optionalField?: string;

    get requestParameters(): string[] {
        return [];
    }

    constructor() {
        super(Object, false);
    }
}
