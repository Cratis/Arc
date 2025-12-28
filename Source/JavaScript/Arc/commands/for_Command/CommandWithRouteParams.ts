// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Command } from '../Command';
import { PropertyDescriptor } from '../../reflection/PropertyDescriptor';

export interface ICommandWithRouteParams {
    id: string;
    name: string;
}

export class CommandWithRouteParams extends Command<ICommandWithRouteParams> implements ICommandWithRouteParams {
    route = '/api/items/{id}';
    propertyDescriptors: PropertyDescriptor[] = [
        new PropertyDescriptor('id', String),
        new PropertyDescriptor('name', String)
    ];

    constructor() {
        super(Object, false);
    }

    get requestParameters(): string[] {
        return ['id'];
    }
    get properties(): string[] {
        return ['id', 'name'];
    }

    id!: string;
    name!: string;
}
