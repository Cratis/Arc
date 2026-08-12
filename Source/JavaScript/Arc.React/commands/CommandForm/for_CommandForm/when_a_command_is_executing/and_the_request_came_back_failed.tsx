// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from '../../../../given';
import { an_executing_command_form } from '../given/an_executing_command_form';

/**
 * A transport failure does not reject - performRequest catches it and answers with a failed result. The form still has to stop reporting an execution that
 * has ended, whichever way it ended.
 */
describe('when a command is executing and the request came back failed', given(an_executing_command_form, context => {
    beforeEach(async () => {
        context.renderForm();
        await context.beginExecute();
        await context.failExecute();
    });
    afterEach(() => context.cleanup());

    it('should stop reporting that it is executing', () => context.lastRenderPropReading.should.be.false);
}));
