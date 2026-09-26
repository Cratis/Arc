// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { a_command } from './given/a_command.js';
import { given } from '../../given.js';
import { Globals } from '../../Globals.js';
import { SomeCommand } from './SomeCommand.js';

describe('when constructing command with globals api base path', given(a_command, () => {
    let originalApiBasePath: string | undefined;
    let command: SomeCommand;

    beforeEach(() => {
        originalApiBasePath = Globals.apiBasePath;
        Globals.apiBasePath = '/custom/api';
        command = new SomeCommand();
    });

    afterEach(() => {
        if (originalApiBasePath !== undefined) {
            Globals.apiBasePath = originalApiBasePath;
        }
    });

    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    it('should initialize with globals api base path', () => (command as any)._apiBasePath.should.equal('/custom/api'));
}));
