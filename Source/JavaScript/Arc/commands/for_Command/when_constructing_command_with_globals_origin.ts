// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { a_command } from './given/a_command';
import { given } from '../../given';
import { Globals } from '../../Globals';
import { SomeCommand } from './SomeCommand';

describe('when constructing command with globals origin', given(a_command, () => {
    let originalOrigin: string | undefined;
    let command: SomeCommand;

    beforeEach(() => {
        originalOrigin = Globals.origin;
        Globals.origin = 'https://example.com';
        command = new SomeCommand();
    });

    afterEach(() => {
        if (originalOrigin !== undefined) {
            Globals.origin = originalOrigin;
        }
    });

    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    it('should initialize with globals origin', () => (command as any)._origin.should.equal('https://example.com'));
}));
