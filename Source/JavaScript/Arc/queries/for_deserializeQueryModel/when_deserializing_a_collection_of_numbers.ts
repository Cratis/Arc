// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Constructor } from '@cratis/fundamentals';
import { deserializeQueryModels } from '../deserializeQueryModel';

describe('when deserializing a collection of numbers', () => {
    const result = deserializeQueryModels<number>(Number as Constructor, [42, 43]);

    it('should keep every item a primitive number', () => result.every(_ => typeof _ === 'number').should.be.true);
    it('should keep the values intact', () => result.should.deep.equal([42, 43]));
});
