// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Command } from '../Command';
import { PropertyDescriptor } from '../../reflection/PropertyDescriptor';

export interface ISomeCommand {
    someProperty: string;
}

export class SomeCommand extends Command<ISomeCommand> implements ISomeCommand {
    route = '';
    propertyDescriptors: PropertyDescriptor[] = [
        new PropertyDescriptor('someProperty', String)
    ];

    constructor() {
        super(Object, false);
    }

    get requestParameters(): string[] {
        return [];
    }
    get properties(): string[] {
        return [
            'someProperty'
        ];
    }

    someProperty!: string;
}

