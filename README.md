# SQLBasic.net

> **English** | [Japanese](README.ja.md) | [Simplified Chinese](README.zh-CN.md) | [Spanish](README.es.md)

**SQLBasic.net** is an intuitive SQL editor for **SQLite**.
It provides syntax highlighting, smart auto-completion, and SQL formatting, allowing you to instantly view query results in the same window.
Because it runs entirely **offline without any cloud connection**, setup is minimal—allowing you to focus on learning immediately after installation.

<img width="1234" height="617" alt="image" src="https://github.com/user-attachments/assets/068f58dd-5b3d-4a50-bc96-1b9a6593e69c" />

## Target Users

This application is designed for **beginners who are just starting to learn SQL**.
No database installation or configuration is required.

A dedicated SQLite file is automatically generated at startup, so you can immediately create tables and experiment with basic SQL operations.
Syntax highlighting and smart auto-completion help detect errors early, providing a smooth experience even for first-time users.
Since the application works entirely **offline**, you can study at your own pace without depending on a network environment.

## System Requirements

- Windows 10 / 11
- .NET 8.0 (Windows Desktop)

## Features

### Editor

| Feature | Shortcut | Description |
|---|---|---|
| Syntax highlighting | — | SQL keywords, strings, numbers, and comments are color-coded |
| SQL formatting | `Alt+F` | Formats the entire document |
| Line comment | `Ctrl+K` | Adds `-- ` to the current line or all selected lines |
| Line uncomment | `Shift+Ctrl+K` | Removes `-- ` from the current line or all selected lines |
| Cursor-aware execution | `Ctrl+Enter` | Executes only the query at the cursor position, using semicolons as statement delimiters |
| CSV export | `Ctrl+L` | Exports the current query result to a CSV file |

### Smart Auto-Completion (`Ctrl+Space`)

Auto-completion is context-aware and suggests different candidates depending on where the cursor is.

| Context | Candidates |
|---|---|
| After `FROM` / `JOIN` / `UPDATE` / `INSERT INTO` | Table names |
| After `DROP TABLE` / `ALTER TABLE` / `CREATE TABLE` | Table names (supports `IF EXISTS` / `IF NOT EXISTS`) |
| In `SELECT` column list | Column names of all tables referenced in the query |
| After `table.` | Column names of that specific table |
| After SELECT columns (before `FROM`) | `FROM` |
| Start of a new statement | `SELECT`, `INSERT INTO`, `UPDATE`, `DELETE FROM`, `CREATE TABLE`, etc. |
| After `FROM` table name | `WHERE`, `INNER JOIN`, `LEFT JOIN`, `GROUP BY`, `ORDER BY`, `LIMIT`, etc. |
| After `WHERE` / `AND` / `OR` | `AND`, `OR`, `NOT`, `EXISTS`, `BETWEEN`, `IN`, `LIKE`, `IS`, `NULL` |
| After `GROUP` / `ORDER` | `BY` |
| After `ORDER BY` column | `ASC`, `DESC`, `NULLS FIRST`, `NULLS LAST` |
| After `INNER` / `LEFT` / `RIGHT` / `FULL` | `JOIN`, `OUTER JOIN` |
| After `LIMIT` | `OFFSET` |
| After `CREATE` | `TABLE`, `INDEX`, `VIEW`, `TRIGGER`, `TEMP TABLE` |
| After `DROP` | `TABLE`, `INDEX`, `VIEW`, `TRIGGER` |

### Execution Result Messages

After running a query, a message is shown in the status bar at the bottom of the window.

| Operation | Message |
|---|---|
| `SELECT` | Number of rows returned |
| `INSERT` | Number of rows inserted |
| `UPDATE` | Number of rows updated |
| `DELETE` | Number of rows deleted |
| `CREATE TABLE` / `CREATE INDEX` | Object type and name that was created |
| `DROP TABLE` / `DROP INDEX` | Object type and name that was dropped |

### Database Panel (Right Sidebar)

- **Table list** — Displays all tables in the connected SQLite database
- **Column info** — Select a table to view its columns, data types, and nullable status
- **Connect to another DB** — Switch to a different SQLite database file at any time

### Other

- **History management** — Browse and re-run previously executed queries
- **Query templates** — Reuse frequently used SQL snippets
- **Multiple windows** — Open multiple editor windows to compare queries side by side
- **Auto-creates DB** — A SQLite database file is automatically created on first launch

## Keyboard Shortcuts

| Shortcut | Action |
|---|---|
| `Ctrl+Enter` | Execute query at cursor position |
| `Ctrl+Space` | Open auto-completion |
| `Ctrl+K` | Add line comment (`-- `) |
| `Shift+Ctrl+K` | Remove line comment |
| `Alt+F` | Format SQL document |
| `Ctrl+L` | Export results to CSV |

## How to Launch

When launched, the program automatically creates an SQLite database file at a fixed path.
You can then write and execute SQL queries directly in the built-in editor.

To connect to a different SQLite file, click the **"Connect to another DB"** button in the right sidebar.
