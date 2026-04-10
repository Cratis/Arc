// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import 'reflect-metadata';
import { injectable } from 'tsyringe';
import { props as propsDecorator, propsTypeKey } from '../../props';

class ComponentProps {
    title: string = '';
    isReadOnly: boolean = false;
}

@injectable()
class MyViewModel {
    constructor(@propsDecorator componentProps: ComponentProps) {
        void componentProps;
    }
}

describe('when decorating constructor parameter with typed class', () => {
    it('should store the props type on the view model constructor', () => {
        const storedType = (MyViewModel as unknown as Record<string, unknown>)[propsTypeKey];
        storedType!.should.equal(ComponentProps);
    });
});
