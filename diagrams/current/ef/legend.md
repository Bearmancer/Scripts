# EF Core Current Schema Legend

## 🛠️ PostgreSQL Types
| Type | Description |
| :--- | :--- |
| `int` | 4-byte signed integer |
| `long` | 8-byte signed integer (`bigint`) |
| `text` | Variable-length character string |
| `varchar(N)` | Variable-length character string with limit `N` |
| `date` | Calendar date (year, month, day) |
| `timestamptz` | Timestamp with time zone (UTC) |
| `jsonb` | Binary JSON data |
| `uuid` | Universally Unique Identifier |

## 🔑 Constraints & Keywords
- **PK**: Primary Key - uniquely identifies each record in the table.
- **FK**: Foreign Key - establishes a relationship to another table.
- **UNIQUE**: Ensures all values in the column are distinct.
- **NOT NULL**: Column must contain a value (implied for PKs).
- **RESTRICT**: Prevents deletion of a referenced record if dependent records exist.

## 📦 Schema Organization
The database is divided into schemas to isolate domain concerns:
- `youtube`: Scraped YouTube metadata.
- `music`: Core music entities (Artists, Albums, Tracks, Scrobbles).
- `classical`: Classical music specific structures (Movements).
- `fibery`: Legacy Fibery entity tracking.
- `public`: Infrastructure, logs, and general source records.
