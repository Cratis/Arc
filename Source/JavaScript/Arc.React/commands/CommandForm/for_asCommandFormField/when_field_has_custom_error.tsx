// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { renderFieldWithErrors } from './given/a_field_rendered_with_errors.js';

describe('when field has custom error', () => {
    let errors: string[];

    beforeEach(() => {
        errors = renderFieldWithErrors('First Name is required.', { requiredField: 'First Name is required.' });
    });

    it('should report the error once', () => errors.should.deep.equal(['First Name is required.']));
});
