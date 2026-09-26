// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { an_observable_query_for } from './given/an_observable_query_for.js';
import { given } from '../../given.js';
import { Sorting } from '../Sorting.js';
import { Paging } from '../Paging.js';


describe('when constructing enumerable query', given(an_observable_query_for, context => {
    it('should set sorting to none', () => context.enumerableQuery.sorting.should.equal(Sorting.none));
    it('should set paging to no paging', () => context.enumerableQuery.paging.should.equal(Paging.noPaging));
    it('should set model type to String', () => context.enumerableQuery.modelType.should.equal(String));
    it('should set enumerable to true', () => context.enumerableQuery.enumerable.should.be.true);
    it('should have default required request parameters', () => context.enumerableQuery.requiredRequestParameters.should.deep.equal(['category']));
    it('should have default route', () => context.enumerableQuery.route.should.equal('/api/items/{category}'));
    it('should have default value as empty array', () => context.enumerableQuery.defaultValue.should.deep.equal([]));
}));