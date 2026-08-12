// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from '../../../../given';
import { an_executing_command_form } from '../given/an_executing_command_form';

describe('when a command is executing and it has not started', given(an_executing_command_form, context => {
    beforeEach(() => context.renderForm());
    afterEach(() => context.cleanup());

    it('should not report that it is executing', () => context.lastRenderPropReading.should.be.false);
}));
