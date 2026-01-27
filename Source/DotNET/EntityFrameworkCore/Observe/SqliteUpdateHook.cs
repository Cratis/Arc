// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Cratis.Arc.EntityFrameworkCore.Observe;

/// <summary>
/// P/Invoke definitions for SQLite update hook functionality.
/// </summary>
internal static partial class SqliteUpdateHook
{
    /// <summary>
    /// Delegate for the SQLite update hook callback.
    /// </summary>
    /// <param name="userData">User data pointer passed to sqlite3_update_hook.</param>
    /// <param name="action">The type of operation (INSERT, UPDATE, DELETE).</param>
    /// <param name="databaseName">The database name (usually "main").</param>
    /// <param name="tableName">The name of the table that was modified.</param>
    /// <param name="rowId">The rowid of the affected row.</param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void UpdateHookCallback(
        IntPtr userData,
        int action,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string databaseName,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string tableName,
        long rowId);

    /// <summary>
    /// The operation type for SQLite update hook callbacks.
    /// </summary>
    internal enum SqliteAction
    {
        /// <summary>
        /// Delete operation.
        /// </summary>
        Delete = 9,

        /// <summary>
        /// Insert operation.
        /// </summary>
        Insert = 18,

        /// <summary>
        /// Update operation.
        /// </summary>
        Update = 23
    }

    /// <summary>
    /// Registers an update hook callback with the SQLite connection.
    /// </summary>
    /// <param name="db">The SQLite database connection handle.</param>
    /// <param name="callback">The callback function to invoke on changes.</param>
    /// <param name="userData">User data pointer passed to the callback.</param>
    /// <returns>The previous callback pointer (if any).</returns>
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    [LibraryImport("e_sqlite3", EntryPoint = "sqlite3_update_hook")]
    internal static partial IntPtr RegisterUpdateHook(IntPtr db, UpdateHookCallback? callback, IntPtr userData);
}
