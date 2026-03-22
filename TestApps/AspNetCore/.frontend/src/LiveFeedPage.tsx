// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState } from 'react';
import { All } from 'Features/AspNetCore/All';
import { ByAuthor } from 'Features/AspNetCore/ByAuthor';
import { PostToFeed } from 'Features/AspNetCore/PostToFeed';

/**
 * Page that demonstrates the LiveFeed observable query and PostToFeed command.
 *
 * Shows three panels:
 *  - All messages (subscribes to LiveFeed.All via SSE)
 *  - Messages filtered by author (subscribes to LiveFeed.ByAuthor via SSE)
 *  - A form to post a new message, which triggers a push to all subscribers
 */
export const LiveFeedPage = () => {
    const [allResult] = All.use();
    const [authorFilter, setAuthorFilter] = useState('');
    const [byAuthorResult] = ByAuthor.when(authorFilter.length > 0).use({ author: authorFilter });
    const [command, setValues] = PostToFeed.use();
    const [postError, setPostError] = useState('');
    const [postSuccess, setPostSuccess] = useState('');

    const handlePost = async () => {
        setPostError('');
        setPostSuccess('');
        const result = await command.execute();
        if (result.isSuccess) {
            setPostSuccess(`Posted at ${new Date(result.response?.postedAt ?? '').toLocaleTimeString()}`);
            setValues({ author: '', text: '' });
        } else {
            setPostError('Failed to post. Check the backend.');
        }
    };

    return (
        <div>
            <h2>Live Feed</h2>

            <section style={{ marginBottom: 32 }}>
                <h3>Post a message</h3>
                <p>
                    Executes the <code>PostToFeed</code> command, which appends a message and
                    pushes the updated list to all SSE subscribers.
                </p>
                <div style={{ display: 'flex', flexDirection: 'column', gap: 8, maxWidth: 400 }}>
                    <input
                        placeholder="Author"
                        value={command.author ?? ''}
                        onChange={event => setValues({ author: event.target.value })}
                    />
                    <input
                        placeholder="Message"
                        value={command.text ?? ''}
                        onChange={event => setValues({ text: event.target.value })}
                    />
                    <button
                        onClick={handlePost}
                        disabled={!command.author || !command.text}
                    >
                        Post
                    </button>
                    {postSuccess && <p style={{ color: 'green' }}>{postSuccess}</p>}
                    {postError && <p style={{ color: 'red' }}>{postError}</p>}
                </div>
            </section>

            <section style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 24 }}>
                <div>
                    <h3>All messages (SSE)</h3>
                    <p>Subscribes to <code>LiveFeed.All()</code> via SSE.</p>
                    {allResult.isPerforming && <p>Connecting…</p>}
                    <MessageList messages={allResult.data ?? []} />
                </div>

                <div>
                    <h3>Filter by author</h3>
                    <p>Subscribes to <code>LiveFeed.ByAuthor(author)</code> via SSE when author is set.</p>
                    <input
                        placeholder="Author to filter"
                        value={authorFilter}
                        onChange={event => setAuthorFilter(event.target.value)}
                        style={{ marginBottom: 8, width: '100%', boxSizing: 'border-box' }}
                    />
                    {byAuthorResult.isPerforming && <p>Connecting…</p>}
                    {!authorFilter && <p style={{ color: '#888' }}>Enter an author name above to activate</p>}
                    {authorFilter && <MessageList messages={byAuthorResult.data ?? []} />}
                </div>
            </section>
        </div>
    );
};

interface Message {
    author: string;
    text: string;
    postedAt: Date;
}

interface MessageListProps {
    messages: Message[];
}

const MessageList = ({ messages }: MessageListProps) => {
    if (messages.length === 0) {
        return <p style={{ color: '#888' }}>No messages yet.</p>;
    }

    return (
        <ul style={{ listStyle: 'none', padding: 0, margin: 0 }}>
            {[...messages].reverse().map((message, index) => (
                <li
                    key={index}
                    style={{
                        borderBottom: '1px solid #eee',
                        padding: '8px 0',
                    }}
                >
                    <strong>{message.author}</strong>
                    {' — '}
                    {message.text}
                    <br />
                    <small style={{ color: '#888' }}>
                        {new Date(message.postedAt).toLocaleTimeString()}
                    </small>
                </li>
            ))}
        </ul>
    );
};
