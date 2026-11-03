// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { QueryInstanceCache } from '../../QueryInstanceCache.js';
import { QueryResultWithState } from '../../QueryResultWithState.js';
import { PagingInfo } from '../../PagingInfo.js';
import { ValidationResult } from '../../../validation/ValidationResult.js';
import { ValidationResultSeverity } from '../../../validation/ValidationResultSeverity.js';

for (const changeSetOnly of [false, true]) {
    describe(`when setting the last result with ${changeSetOnly ? 'only a changed change set' : 'equivalent metadata'}`, () => {
        let listener: sinon.SinonStub;

        const result = () => new QueryResultWithState(
            [{ id: 'a' }],
            { ...new PagingInfo(), totalItems: 1, totalPages: 1 },
            false, true, false,
            [new ValidationResult(ValidationResultSeverity.Error, 'invalid', ['name'], { limit: 1 })],
            true, ['failed'], 'stack', false
        );

        beforeEach(() => {
            const cache = new QueryInstanceCache();
            const key = 'MyQuery::';
            cache.getOrCreate(key, () => ({}));
            cache.setLastResult(key, result());
            listener = sinon.stub();
            cache.addListener(key, listener);
            const next = result();
            if (changeSetOnly) {
                Object.assign(next, { changeSet: { added: [{ id: 'a' }], replaced: [], removed: [] } });
            }
            cache.setLastResult(key, next);
        });

        it('should suppress the notification', () => listener.notCalled.should.be.true);
    });
}
