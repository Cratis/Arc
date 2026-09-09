// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { act } from '@testing-library/react';
import { CommandResult } from '@cratis/arc/commands';
import { given } from '../../../../given';
import { a_command_form_with_exception_feedback } from '../given/a_command_form_with_exception_feedback';

describe('when displaying exception feedback and execution succeeds after failure', given(a_command_form_with_exception_feedback, context => {
    let previousMessage: string;

    beforeEach(async () => {
        await context.renderForm();
        context.formContext.commandInstance.execute = async () => context.exceptionResult();
        await act(async () => { await context.formContext.onExecute!(); });
        previousMessage = context.rendered.container.textContent!;
        context.formContext.commandInstance.execute = async () => CommandResult.empty;
        await act(async () => { await context.formContext.onExecute!(); });
    });

    it('should replace the previous exception feedback with no error display', () => {
        previousMessage.should.equal(context.defaultMessage);
        context.rendered.container.textContent!.should.equal('');
        (context.rendered.queryByRole('alert') === null).should.be.true;
    });
    it('should keep the successful result in context', () => {
        context.formContext.commandResult!.should.equal(CommandResult.empty);
    });
}));
