# SQLBasic.net

> [English](README.md) | [日本語](README.ja.md) | **简体中文** | [Español](README.es.md)

**SQLBasic.net** 是一款面向 **SQLite** 的直观 SQL 编辑器。
提供语法高亮、智能自动补全和 SQL 格式化功能，可在同一窗口中即时查看查询结果。
由于完全**离线运行，无需任何云连接**，安装后即可立即开始学习，无需繁琐配置。

<img width="1234" height="617" alt="image" src="https://github.com/user-attachments/assets/068f58dd-5b3d-4a50-bc96-1b9a6593e69c" />

## 目标用户

本应用主要面向**刚开始学习 SQL 的初学者**。
无需安装或配置任何数据库。

启动时会自动生成专用的 SQLite 文件，让您可以立即创建表格并体验基本的 SQL 操作。
语法高亮和智能自动补全有助于及早发现错误，即使是初次使用的用户也能顺畅操作。
由于完全**离线**运行，您可以不依赖网络环境，按照自己的节奏学习。

## 系统要求

- Windows 10 / 11
- .NET 8.0（Windows 桌面版）

## 功能列表

### 编辑器

| 功能 | 快捷键 | 说明 |
|---|---|---|
| 语法高亮 | — | SQL 关键字、字符串、数字和注释分别用不同颜色显示 |
| SQL 格式化 | `Alt+F` | 格式化整个文档 |
| 添加行注释 | `Ctrl+K` | 在当前行或所有选中行的行首添加 `-- ` |
| 取消行注释 | `Shift+Ctrl+K` | 从当前行或所有选中行的行首删除 `-- ` |
| 光标位置执行 | `Ctrl+Enter` | 仅执行光标所在位置的查询，以分号作为语句分隔符 |
| CSV 导出 | `Ctrl+L` | 将当前查询结果导出为 CSV 文件 |

### 智能自动补全（`Ctrl+Space`）

自动补全能够感知上下文，根据光标位置提供不同的候选项。

| 光标位置 | 候选项 |
|---|---|
| `FROM` / `JOIN` / `UPDATE` / `INSERT INTO` 之后 | 表名 |
| `DROP TABLE` / `ALTER TABLE` / `CREATE TABLE` 之后 | 表名（支持 `IF EXISTS` / `IF NOT EXISTS`） |
| `SELECT` 列列表中 | 查询中引用的所有表的列名 |
| `表名.` 之后 | 该表的列名 |
| SELECT 列名之后（`FROM` 之前） | `FROM` |
| 新语句开头 | `SELECT`、`INSERT INTO`、`UPDATE`、`DELETE FROM`、`CREATE TABLE` 等 |
| `FROM` 表名之后 | `WHERE`、`INNER JOIN`、`LEFT JOIN`、`GROUP BY`、`ORDER BY`、`LIMIT` 等 |
| `WHERE` / `AND` / `OR` 之后 | `AND`、`OR`、`NOT`、`EXISTS`、`BETWEEN`、`IN`、`LIKE`、`IS`、`NULL` |
| `GROUP` / `ORDER` 之后 | `BY` |
| `ORDER BY` 列名之后 | `ASC`、`DESC`、`NULLS FIRST`、`NULLS LAST` |
| `INNER` / `LEFT` / `RIGHT` / `FULL` 之后 | `JOIN`、`OUTER JOIN` |
| `LIMIT` 之后 | `OFFSET` |
| `CREATE` 之后 | `TABLE`、`INDEX`、`VIEW`、`TRIGGER`、`TEMP TABLE` |
| `DROP` 之后 | `TABLE`、`INDEX`、`VIEW`、`TRIGGER` |

### 执行结果消息

运行查询后，窗口底部的状态栏会显示结果消息。

| 操作 | 消息 |
|---|---|
| `SELECT` | 返回的行数 |
| `INSERT` | 插入的行数 |
| `UPDATE` | 更新的行数 |
| `DELETE` | 删除的行数 |
| `CREATE TABLE` / `CREATE INDEX` | 创建的对象类型和名称 |
| `DROP TABLE` / `DROP INDEX` | 删除的对象类型和名称 |

### 数据库面板（右侧边栏）

- **表列表** — 显示当前连接的 SQLite 数据库中的所有表
- **列信息** — 选择表后，显示其列名、数据类型和是否允许 NULL
- **连接其他数据库** — 随时切换到不同的 SQLite 数据库文件

### 其他功能

- **历史记录管理** — 浏览并重新执行以前运行过的查询
- **查询模板** — 注册并复用常用的 SQL 代码片段
- **多窗口** — 打开多个编辑器窗口，并排比较查询
- **自动创建数据库** — 首次启动时自动创建 SQLite 数据库文件

## 键盘快捷键

| 快捷键 | 操作 |
|---|---|
| `Ctrl+Enter` | 执行光标位置的查询 |
| `Ctrl+Space` | 打开自动补全 |
| `Ctrl+K` | 添加行注释（`-- `） |
| `Shift+Ctrl+K` | 取消行注释 |
| `Alt+F` | 格式化 SQL 文档 |
| `Ctrl+L` | 将结果导出为 CSV |

## 启动方式

启动后，程序会自动在固定路径创建一个 SQLite 数据库文件。
之后，您可以直接在内置编辑器中编写并执行 SQL 查询。

若要连接其他 SQLite 文件，请点击右侧边栏中的**「连接其他数据库」**按钮。
