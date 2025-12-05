// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Bindings } from '../Bindings';
import { Globals } from '@cratis/arc';
import { bindings_context } from './given/bindings_context';
import { given } from '../../../Arc/given';

describe('when initializing bindings', given(bindings_context, () => {
    let originalMicroservice: string;
    let originalApiBasePath: string;
    let originalOrigin: string;

    beforeEach(() => {
        originalMicroservice = Globals.microservice;
        originalApiBasePath = Globals.apiBasePath;
        originalOrigin = Globals.origin;

        Bindings.initialize('test-microservice', '/test/api', 'http://test.com');
    });

    afterEach(() => {
        Globals.microservice = originalMicroservice;
        Globals.apiBasePath = originalApiBasePath;
        Globals.origin = originalOrigin;
    });

    it('should set globals microservice', () => Globals.microservice.should.equal('test-microservice'));
    it('should set globals api base path', () => Globals.apiBasePath.should.equal('/test/api'));
    it('should set globals origin', () => Globals.origin.should.equal('http://test.com'));
}));
