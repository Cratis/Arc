// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { QueryInstanceCache } from '../../QueryInstanceCache.js';
import { QueryResultWithState } from '../../QueryResultWithState.js';
import { PagingInfo } from '../../PagingInfo.js';
import { ValidationResult } from '../../../validation/ValidationResult.js';
import { ValidationResultSeverity } from '../../../validation/ValidationResultSeverity.js';
import { ValidationResultReason } from '../../../validation/ValidationResultReason.js';

type Item = { id: string };
const validation = new ValidationResult(ValidationResultSeverity.Error, 'first', ['name'], { limit: 1 });
const changes: [string, Partial<QueryResultWithState<Item[]>>][] = [
    ['paging total items', { paging: { ...new PagingInfo(), totalItems: 2 } }],
    ['paging total pages', { paging: { ...new PagingInfo(), totalPages: 2 } }],
    ['paging page', { paging: { ...new PagingInfo(), page: 1 } }],
    ['paging size', { paging: { ...new PagingInfo(), size: 10 } }],
    ['authorization', { isAuthorized: false }],
    ['validity', { isValid: false }],
    ['exception flag', { hasExceptions: true }],
    ['exception messages', { exceptionMessages: ['second'] }],
    ['exception stack trace', { exceptionStackTrace: 'second stack' }],
    ['performing state', { isPerforming: true }],
    ['validation message', { validationResults: [{ ...validation, message: 'second' }] }],
    ['validation severity', { validationResults: [{ ...validation, severity: ValidationResultSeverity.Warning }] }],
    ['validation members', { validationResults: [{ ...validation, members: ['other'] }] }],
    ['validation state', { validationResults: [{ ...validation, state: { limit: 2 } }] }],
    ['validation reason', { validationResults: [{ ...validation, reason: ValidationResultReason.ConstraintViolation }] }],
    ['validation reason detail', { validationResults: [{ ...validation, reasonDetail: 'required' }] }]
];

for (const [field, change] of changes) {
    describe(`when setting the last result with changed ${field}`, () => {
        const key = 'MyQuery::';
        let cache: QueryInstanceCache;
        let listener: sinon.SinonStub;
        let first: QueryResultWithState<Item[]>;

        beforeEach(() => {
            cache = new QueryInstanceCache();
            cache.getOrCreate(key, () => ({}));
            first = Object.assign(QueryResultWithState.empty([{ id: 'a' }]), {
                isSuccess: false,
                validationResults: [validation],
                exceptionMessages: ['first']
            });
            cache.setLastResult(key, first);
            listener = sinon.stub();
            cache.addListener(key, listener);
            const next = Object.assign(QueryResultWithState.empty([{ id: 'a' }]), first, { data: [{ id: 'a' }] }, change);
            cache.setLastResult(key, next);
        });

        it('should notify the listener', () => listener.calledOnce.should.be.true);
        it('should preserve unchanged row references', () => cache.getLastResult<Item[]>(key)!.data[0].should.equal(first.data[0]));
    });
}
