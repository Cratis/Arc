// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as sinon from 'sinon';
import { createFetchHelper } from '../../../helpers/fetchHelper';
import { executeQueryHttpRequest, resetQueryHttpMethodResolution } from '../../QueryHttpRequest';
import { QueryHttpMethod } from '../../QueryHttpMethod';
import { makeOptions } from '../options';

describe('when performing with auto and an aborted request', () => {
    let fetchStub: sinon.SinonStub;
    let fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };
    let error: { name?: string } | undefined;

    beforeEach(async () => {
        resetQueryHttpMethodResolution();
        fetchHelper = createFetchHelper();
        fetchStub = fetchHelper.stubFetch();
        fetchStub.callsFake(() => Promise.reject(Object.assign(new Error('aborted'), { name: 'AbortError' })));
        try {
            await executeQueryHttpRequest(QueryHttpMethod.Auto, makeOptions());
        } catch (caught) {
            error = caught as { name?: string };
        }
    });

    afterEach(() => fetchHelper.restore());

    it('should rethrow the abort error', () => error!.name!.should.equal('AbortError'));
    it('should not fall back to GET', () => fetchStub.callCount.should.equal(1));
});
