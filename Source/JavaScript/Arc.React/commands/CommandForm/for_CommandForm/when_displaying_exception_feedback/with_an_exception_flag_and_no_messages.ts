// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { act } from '@testing-library/react';
import { given } from '../../../../given';
import { a_command_form_with_exception_feedback } from '../given/a_command_form_with_exception_feedback';

describe('when displaying exception feedback with an exception flag and no messages', given(a_command_form_with_exception_feedback, context => {
    beforeEach(async () => {
        await context.renderForm();
        await act(async () => context.setResult(context.exceptionResult(true, [])));
    });

    it('should display the safe default even without diagnostic messages', () => {
        context.rendered.getByRole('alert').textContent!.should.equal(context.defaultMessage);
    });
}));
