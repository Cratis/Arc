// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { a_command } from './given/a_command';
import { given } from '../../given';
import { Globals } from '../../Globals';
import { SomeCommand } from './SomeCommand';

describe('when constructing command', given(a_command, () => {
    let originalMicroservice: string | undefined;
    let originalApiBasePath: string | undefined;
    let originalOrigin: string | undefined;
    let command: SomeCommand;

    beforeEach(() => {
        originalMicroservice = Globals.microservice;
        originalApiBasePath = Globals.apiBasePath;
        originalOrigin = Globals.origin;
        Globals.microservice = 'test-microservice';
        Globals.apiBasePath = '/test-api';
        Globals.origin = 'http://test-origin';
        command = new SomeCommand();
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

    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    it('should initialize with globals microservice', () => (command as any)._microservice.should.equal('test-microservice'));
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    it('should initialize with globals api base path', () => (command as any)._apiBasePath.should.equal('/test-api'));
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    it('should initialize with globals origin', () => (command as any)._origin.should.equal('http://test-origin'));
}));
