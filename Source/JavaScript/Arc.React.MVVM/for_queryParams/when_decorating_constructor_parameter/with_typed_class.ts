// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import 'reflect-metadata';
import { injectable } from 'tsyringe';
import { queryParams, queryParamsTypeKey } from '../../queryParams';

class QueryParamsType {
    filter: string = '';
    page: number = 0;
}

@injectable()
class MyViewModel {
    constructor(@queryParams searchParams: QueryParamsType) {
        void searchParams;
    }
}

describe('when decorating constructor parameter with typed class', () => {
    it('should store the query params type on the view model constructor', () => {
        const storedType = (MyViewModel as unknown as Record<string, unknown>)[queryParamsTypeKey];
        storedType!.should.equal(QueryParamsType);
    });
});
