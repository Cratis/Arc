// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { TestObservableQuery } from './given/TestQueries.js';


describe('when setting api base path', () => {
    let query: TestObservableQuery;

    beforeEach(() => {
        query = new TestObservableQuery();
    });

    it('should not throw when setting api base path', () => {
        (() => query.setApiBasePath('/api/v1')).should.not.throw();
    });
});