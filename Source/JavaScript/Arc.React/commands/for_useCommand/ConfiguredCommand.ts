// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Command } from '@cratis/arc/commands';
import { PropertyDescriptor } from '@cratis/arc/reflection';

export class ConfiguredCommand extends Command<{ name: string }> {
    route = '/save';
    get requestParameters(): string[] {
        return [];
    }

    get properties(): string[] {
        return ['name'];
    }

    propertyDescriptors = [new PropertyDescriptor('name', String)];
    name = '';

    constructor() {
        super(Object, false);
    }
}
