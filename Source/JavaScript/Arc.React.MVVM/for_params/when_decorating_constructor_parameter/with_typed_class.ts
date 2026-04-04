// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import 'reflect-metadata';
import { injectable } from 'tsyringe';
import { params, routeParamsTypeKey } from '../../params';

class RouteParamsType {
    id: string = '';
    name: string = '';
}

@injectable()
class MyViewModel {
    constructor(@params routeParams: RouteParamsType) {
        void routeParams;
    }
}

describe('when decorating constructor parameter with typed class', () => {
    it('should store the route params type on the view model constructor', () => {
        const storedType = (MyViewModel as unknown as Record<string, unknown>)[routeParamsTypeKey];
        storedType!.should.equal(RouteParamsType);
    });
});
