// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Command } from '../Command.js';
import { PropertyDescriptor } from '../../reflection/PropertyDescriptor.js';

export class GeneratedCommand extends Command<{ value: unknown }> {
    readonly route = '/save';
    readonly propertyDescriptors = [new PropertyDescriptor('value', Object, true)];
    #value: unknown;

    constructor() {
        super(Object, false);
    }

    get requestParameters(): string[] {
        return [];
    }

    get properties(): string[] {
        return ['value'];
    }

    get value(): unknown {
        return this.#value;
    }

    set value(value: unknown) {
        this.#value = value;
        this.propertyChanged('value');
    }
}
