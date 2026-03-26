// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Bindings } from '../Bindings';
import { Globals } from '@cratis/arc';
import { bindings_context } from './given/bindings_context';
import { given } from '../given';

describe('when initializing bindings with query direct mode enabled', given(bindings_context, () => {
    let originalQueryDirectMode: boolean;

    beforeEach(() => {
        originalQueryDirectMode = Globals.queryDirectMode;
        Bindings.initialize('test-microservice', '/test/api', 'http://test.com', undefined, undefined, undefined, true);
    });

    afterEach(() => {
        Globals.queryDirectMode = originalQueryDirectMode;
    });

    it('should set globals query direct mode to true', () => Globals.queryDirectMode.should.be.true);
}));
