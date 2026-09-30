// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { renderFieldWithErrors } from './given/a_field_rendered_with_errors.js';

describe('when field has server error only', () => {
    let errors: string[];

    beforeEach(() => {
        errors = renderFieldWithErrors('Server says no.', {});
    });

    it('should report the server error', () => errors.should.deep.equal(['Server says no.']));
});
