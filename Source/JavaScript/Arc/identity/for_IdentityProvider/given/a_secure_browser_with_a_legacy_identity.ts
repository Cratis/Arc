// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { a_browser_with_a_legacy_identity } from './a_browser_with_a_legacy_identity.js';

export class a_secure_browser_with_a_legacy_identity extends a_browser_with_a_legacy_identity {
    protected browserUrl = 'https://identity.example/nested/page';
}
