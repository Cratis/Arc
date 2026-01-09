// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Command, CommandValidator } from '@cratis/arc/commands';
import { PropertyDescriptor } from '@cratis/arc/reflection';

export interface FakeCommandContent {
    someProperty?: string;
    anotherProperty?: number;
}

export class FakeCommand extends Command<FakeCommandContent> {
    readonly route = '/api/fake-command';
    readonly validation = {} as CommandValidator;
    readonly propertyDescriptors: PropertyDescriptor[] = [
        new PropertyDescriptor('someProperty', String, true),
        new PropertyDescriptor('anotherProperty', Number, true)
    ];

    someProperty?: string;
    anotherProperty?: number;

    get requestParameters(): string[] {
        return [];
    }

    constructor() {
        super(Object, false);
    }
}
