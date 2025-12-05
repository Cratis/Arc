// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Bindings } from '../Bindings';
import { Globals } from "@cratis/arc";
import { bindings_context } from "./given/bindings_context";
import { given } from '../given';

describe('when initializing bindings without optional parameters', given(bindings_context, () => {
    let originalMicroservice: string;
    let originalApiBasePath: string;
    let originalOrigin: string;

    beforeEach(() => {
        originalMicroservice = Globals.microservice;
        originalApiBasePath = Globals.apiBasePath;
        originalOrigin = Globals.origin;

        Bindings.initialize('test-microservice');
    });

    afterEach(() => {
        Globals.microservice = originalMicroservice;
        Globals.apiBasePath = originalApiBasePath;
        Globals.origin = originalOrigin;
    });

    it('should set globals microservice', () => Globals.microservice.should.equal('test-microservice'));
    it('should set globals api base path to empty string', () => Globals.apiBasePath.should.equal(''));
    it('should set globals origin to empty string', () => Globals.origin.should.equal(''));
}));
