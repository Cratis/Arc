// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { TestObservableQuery } from './given/TestQueries.js';


describe('when setting microservice', () => {
    let query: TestObservableQuery;

    beforeEach(() => {
        query = new TestObservableQuery();
    });

    it('should not throw when setting microservice', () => {
        (() => query.setMicroservice('new-microservice')).should.not.throw();
    });
});