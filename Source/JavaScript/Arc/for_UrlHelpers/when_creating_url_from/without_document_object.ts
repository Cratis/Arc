// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { UrlHelpers } from '../../UrlHelpers';


describe("without_document_object", () => {
    let origin: string;
    let apiBasePath: string;
    let route: string;
    let originalDocument: Document | undefined;

    beforeEach(() => {
        // Mock document to be undefined
        originalDocument = global.document;
        (global as { document?: Document }).document = undefined;

        origin = '';
        apiBasePath = '/api/v1';
        route = '/users/123';
    });

    afterEach(() => {
        if (originalDocument) {
            global.document = originalDocument;
        } else {
            delete (global as { document?: Document }).document;
        }
    });

    it("should throw invalid url error", () => {
        (() => UrlHelpers.createUrlFrom(origin, apiBasePath, route)).should.throw('Invalid URL');
    });
});