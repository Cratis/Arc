// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { vi } from 'vitest';
import type { HierarchyProject } from '../../api';

/** One project, one assembly document, one module below it and one feature below that. */
export const hierarchy: HierarchyProject[] = [{
    id: 'Acme.Orders',
    name: 'Acme.Orders',
    documents: [
        { id: 'Acme.Orders', title: 'Acme.Orders', namespace: 'Acme.Orders', kind: 'assembly', parentId: null, resourceName: 'Acme.Orders.play' },
        { id: 'Acme.Orders.Ordering', title: 'Ordering', namespace: 'Acme.Orders.Ordering', kind: 'module', parentId: 'Acme.Orders', resourceName: 'Ordering.play' },
        { id: 'Acme.Orders.Ordering.Checkout', title: 'Checkout', namespace: 'Acme.Orders.Ordering.Checkout', kind: 'feature', parentId: 'Acme.Orders.Ordering', resourceName: 'Checkout.play' }
    ]
}];

export const modelFor = (name: string) => ({
    id: '00000000-0000-4000-8000-000000000001',
    name,
    collections: [],
    stickyNotes: [],
    links: []
});

export interface StubbedRequest {
    url: string;
    signal: AbortSignal;
}

export type Responder = (request: StubbedRequest) => Response | Promise<Response>;

/** Replaces fetch with the given responder and records every request made. */
export const stubFetch = (responder: Responder) => {
    const requests: StubbedRequest[] = [];
    const fetchStub = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
        const request = { url: input.toString(), signal: init?.signal as AbortSignal };
        requests.push(request);
        return Promise.resolve(responder(request));
    });
    vi.stubGlobal('fetch', fetchStub);
    return requests;
};

export const json = (body: unknown) => new Response(JSON.stringify(body), { status: 200, headers: { 'content-type': 'application/json' } });
export const text = (body: string) => new Response(body, { status: 200, headers: { 'content-type': 'text/plain' } });
export const failure = (status: number, statusText: string) => new Response('', { status, statusText });

/** Answers the hierarchy, source and model endpoints from the fixtures above. */
export const respondWithFixtures = (source = 'module Ordering {}') => stubFetch(({ url }) => {
    if (url.endsWith('/hierarchy')) return json(hierarchy);
    if (url.endsWith('/source')) return text(source);
    if (url.endsWith('/model')) return json({ ...modelFor('Acme.Orders'), warnings: ['Screens are not shown on the canvas'] });
    return failure(404, 'Not Found');
});
