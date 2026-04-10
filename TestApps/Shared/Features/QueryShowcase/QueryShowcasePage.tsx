// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState } from 'react';
import { Latest, All, ById, GetAll } from './ShowcaseItem';

/**
 * Page demonstrating the four distinct query types supported by Arc.
 *
 * | # | Kind        | Returns    | API                       |
 * |---|-------------|------------|---------------------------|
 * | 1 | Observable  | Single     | ShowcaseItem.Latest()     |
 * | 2 | Observable  | Collection | ShowcaseItem.All()        |
 * | 3 | Regular     | Single     | ShowcaseItem.ById(id)     |
 * | 4 | Regular     | Collection | ShowcaseItem.GetAll()     |
 *
 * The bottom section shows three independent React components all consuming
 * ShowcaseItem.All() — they share the same underlying query instance through
 * the QueryInstanceCache, so all three update in sync with a single SSE push.
 */
export const QueryShowcasePage = () => {
    const [id, setId] = useState(1);

    return (
        <div>
            <h2>Query Showcase</h2>
            <p>
                Demonstrates the four query types supported by Arc — observable single, observable
                collection, regular single, and regular collection — and shows three independent
                components sharing one query instance via the cache.
            </p>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 24, marginBottom: 32 }}>
                <Section title="1 · Observable — single item">
                    <p>
                        <code>ShowcaseItem.Latest()</code> — <code>ISubject&lt;ShowcaseItem&gt;</code>.
                        Updates every 3 seconds.
                    </p>
                    <LatestView />
                </Section>

                <Section title="2 · Observable — collection">
                    <p>
                        <code>ShowcaseItem.All()</code> — <code>ISubject&lt;IEnumerable&lt;ShowcaseItem&gt;&gt;</code>.
                        Updates every 3 seconds.
                    </p>
                    <AllView />
                </Section>

                <Section title="3 · Regular — single item">
                    <p>
                        <code>ShowcaseItem.ById(id)</code> — snapshot query, re-fetched when <code>id</code> changes.
                    </p>
                    <div style={{ display: 'flex', gap: 8, marginBottom: 8, alignItems: 'center' }}>
                        <label>id:</label>
                        <input
                            type="number"
                            value={id}
                            min={1}
                            onChange={event => setId(Number(event.target.value))}
                            style={{ width: 60 }}
                        />
                    </div>
                    <ByIdView id={id} />
                </Section>

                <Section title="4 · Regular — collection">
                    <p>
                        <code>ShowcaseItem.GetAll()</code> — returns a fixed <code>IQueryable&lt;ShowcaseItem&gt;</code> snapshot.
                    </p>
                    <GetAllView />
                </Section>
            </div>

            <h3>Query instance reuse (3 × ShowcaseItem.All)</h3>
            <p>
                The three panels below all call <code>All.use()</code> independently.
                The <strong>QueryInstanceCache</strong> ensures they share a single SSE
                connection; every server push updates all three simultaneously.
            </p>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: 16 }}>
                <Section title="Panel A">
                    <AllView />
                </Section>
                <Section title="Panel B">
                    <AllView />
                </Section>
                <Section title="Panel C">
                    <AllView />
                </Section>
            </div>
        </div>
    );
};

// ----- sub-components --------------------------------------------------------

const LatestView = () => {
    const [result] = Latest.use();

    if (result.isPerforming) return <p>Connecting…</p>;
    if (!result.hasData || result.data.id == null) return <p>No data yet.</p>;

    return (
        <ItemCard id={result.data.id} name={result.data.name} updatedAt={result.data.updatedAt} />
    );
};

const AllView = () => {
    const [result] = All.use();

    if (result.isPerforming) return <p>Connecting…</p>;
    if (!result.hasData) return <p>No data yet.</p>;

    return (
        <ul style={{ listStyle: 'none', padding: 0, margin: 0 }}>
            {(result.data ?? []).map(item => (
                <li key={item.id} style={{ borderBottom: '1px solid #eee', padding: '4px 0' }}>
                    <ItemCard id={item.id} name={item.name} updatedAt={item.updatedAt} />
                </li>
            ))}
        </ul>
    );
};

interface ByIdViewProps {
    id: number;
}

const ByIdView = ({ id }: ByIdViewProps) => {
    const [result] = ById.use({ id });

    if (result.isPerforming) return <p>Loading…</p>;
    if (!result.hasData || result.data.id == null) return <p>No data.</p>;

    return (
        <ItemCard id={result.data.id} name={result.data.name} updatedAt={result.data.updatedAt} />
    );
};

const GetAllView = () => {
    const [result] = GetAll.use();

    if (result.isPerforming) return <p>Loading…</p>;
    if (!result.hasData) return <p>No data.</p>;

    return (
        <ul style={{ listStyle: 'none', padding: 0, margin: 0 }}>
            {(result.data ?? []).map(item => (
                <li key={item.id} style={{ borderBottom: '1px solid #eee', padding: '4px 0' }}>
                    <ItemCard id={item.id} name={item.name} updatedAt={item.updatedAt} />
                </li>
            ))}
        </ul>
    );
};

interface ItemCardProps {
    id: number;
    name: string;
    updatedAt: Date;
}

const formatTime = (value: Date | undefined | null): string => {
    if (value == null) return '—';
    const date = new Date(value);
    return isNaN(date.getTime()) ? '—' : date.toLocaleTimeString();
};

const ItemCard = ({ id, name, updatedAt }: ItemCardProps) => (
    <div>
        <span style={{ color: '#888', fontSize: 11, marginRight: 6 }}>#{id ?? '?'}</span>
        <strong>{name ?? '…'}</strong>
        <span style={{ color: '#aaa', fontSize: 11, marginLeft: 8 }}>
            {formatTime(updatedAt)}
        </span>
    </div>
);

interface SectionProps {
    title: string;
    children: React.ReactNode;
}

const Section = ({ title, children }: SectionProps) => (
    <div style={{ border: '1px solid #ddd', borderRadius: 6, padding: 16 }}>
        <h4 style={{ margin: '0 0 8px' }}>{title}</h4>
        {children}
    </div>
);
