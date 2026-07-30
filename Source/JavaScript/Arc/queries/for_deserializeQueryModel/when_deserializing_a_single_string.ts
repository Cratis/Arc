// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Constructor } from '@cratis/fundamentals';
import { deserializeQueryModel } from '../deserializeQueryModel';

describe('when deserializing a single string', () => {
    const result = deserializeQueryModel<string>(String as Constructor, 'the value');

    it('should keep it a primitive string', () => (typeof result).should.equal('string'));
    it('should keep the value intact', () => result.should.equal('the value'));
});
