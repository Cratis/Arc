// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readEventModelDocument, type EventModel } from '@cratis/event-models';

/** The kinds of documents the embedded catalog holds. */
export type DocumentKind = 'assembly' | 'module' | 'feature';

/** A single embedded Screenplay document in a project's hierarchy. */
export interface HierarchyDocument {
    id: string;
    title: string;
    namespace: string;
    kind: DocumentKind;
    parentId: string | null;
    resourceName: string;
}

/** A project - one assembly - and every document embedded in it. */
export interface HierarchyProject {
    id: string;
    name: string;
    documents: HierarchyDocument[];
}

/** The canvas model of a document, with anything the canvas cannot show reported as a warning. */
export interface DocumentModel {
    document: EventModel;
    warnings: string[];
}

// The application is served under the host's PathBase (/.cratis/event-model/ by default), so every
// endpoint is resolved relative to the document's base URI instead of an absolute path.
const urlFor = (path: string) => new URL(path, document.baseURI).toString();

const documentPath = (projectId: string, documentId: string, part: 'source' | 'model') =>
    `documents/${encodeURIComponent(projectId)}/${encodeURIComponent(documentId)}/${part}`;

const fetchOrThrow = async (path: string, signal: AbortSignal, accept: string) => {
    const response = await fetch(urlFor(path), { signal, headers: { Accept: accept } });
    if (!response.ok) {
        throw new Error(`Request for '${path}' failed with ${response.status} ${response.statusText}`.trim());
    }
    return response;
};

/** Gets every project with its embedded documents. */
export const getHierarchy = async (signal: AbortSignal): Promise<HierarchyProject[]> => {
    const response = await fetchOrThrow('hierarchy', signal, 'application/json');
    const projects = await response.json();
    if (!Array.isArray(projects)) {
        throw new Error('The hierarchy response was not a list of projects');
    }
    return projects as HierarchyProject[];
};

/** Gets the Screenplay source of a document. */
export const getSource = async (projectId: string, documentId: string, signal: AbortSignal): Promise<string> => {
    const response = await fetchOrThrow(documentPath(projectId, documentId, 'source'), signal, 'text/plain');
    return await response.text();
};

const diagnosticText = (diagnostic: unknown): string => {
    if (typeof diagnostic === 'string') return diagnostic;
    if (diagnostic === null || typeof diagnostic !== 'object') {
        throw new Error('The model response contained an invalid diagnostic');
    }
    const { code, message, path, line, column } = diagnostic as Record<string, unknown>;
    if (typeof message !== 'string') throw new Error('The model response contained a diagnostic without a message');
    const location = typeof path === 'string' && path.length > 0
        ? `${path}${typeof line === 'number' ? `:${line}${typeof column === 'number' ? `:${column}` : ''}` : ''}: `
        : '';
    return `${location}${typeof code === 'string' ? `${code}: ` : ''}${message}`;
};

/**
 * Turns the model response into what the board draws. The payload is the public event-model document;
 * warnings travel beside it and are never folded into the document.
 */
export const toDocumentModel = (payload: unknown): DocumentModel => {
    if (payload === null || typeof payload !== 'object') {
        throw new Error('The model response was not an event model document');
    }
    const { warnings, eventModel, success, errors, document: embedded, ...rest } = payload as Record<string, unknown>;
    if (success === false) {
        throw new Error(Array.isArray(errors) ? errors.map(diagnosticText).join('; ') : 'The embedded Screenplay did not compile');
    }
    return {
        document: readEventModelDocument(eventModel ?? embedded ?? rest),
        warnings: Array.isArray(warnings) ? warnings.map(diagnosticText) : []
    };
};

/** Gets the canvas model of a document. */
export const getModel = async (projectId: string, documentId: string, signal: AbortSignal): Promise<DocumentModel> => {
    const response = await fetchOrThrow(documentPath(projectId, documentId, 'model'), signal, 'application/json');
    return toDocumentModel(await response.json());
};
