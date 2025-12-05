// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { an_observable_query_for } from './given/an_observable_query_for';
import { given } from '../../given';
import { Sorting } from '../Sorting';
import { Paging } from '../Paging';
import { Globals } from '../../Globals';


describe('when constructing', given(an_observable_query_for, context => {
    let originalMicroservice: string | undefined;
    let originalApiBasePath: string | undefined;
    let originalOrigin: string | undefined;

    beforeEach(() => {
        originalMicroservice = Globals.microservice;
        originalApiBasePath = Globals.apiBasePath;
        originalOrigin = Globals.origin;
        Globals.microservice = 'test-microservice';
        Globals.apiBasePath = '/test-api';
        Globals.origin = 'http://test-origin';
    });

    afterEach(() => {
        if (originalMicroservice !== undefined) {
            Globals.microservice = originalMicroservice;
        }
        if (originalApiBasePath !== undefined) {
            Globals.apiBasePath = originalApiBasePath;
        }
        if (originalOrigin !== undefined) {
            Globals.origin = originalOrigin;
        }
    });

    it('should set sorting to none', () => context.query.sorting.should.equal(Sorting.none));
    it('should set paging to no paging', () => context.query.paging.should.equal(Paging.noPaging));
    it('should set model type to String', () => context.query.modelType.should.equal(String));
    it('should set enumerable to false', () => context.query.enumerable.should.be.false);
    it('should have default required request parameters', () => context.query.requiredRequestParameters.should.deep.equal(['id']));
    it('should have default route', () => context.query.route.should.equal('/api/test/{id}'));
    it('should have default value as empty string', () => context.query.defaultValue.should.equal(''));
}));