namespace DrSasuMcp.PostgreSQL.PostgreSQL
{
    public partial class PostgreSQLTool
    {
        const string ListTablesQuery = @"
            SELECT table_schema, table_name
            FROM information_schema.tables
            WHERE table_type = 'BASE TABLE'
              AND table_schema NOT IN ('pg_catalog', 'information_schema')
            ORDER BY table_schema, table_name";

        const string TableInfoQuery = @"
            SELECT
                c.oid AS id,
                c.relname AS name,
                n.nspname AS schema,
                obj_description(c.oid, 'pg_class') AS description,
                CASE c.relkind
                    WHEN 'r' THEN 'table'
                    WHEN 'p' THEN 'partitioned table'
                    WHEN 'v' THEN 'view'
                    WHEN 'm' THEN 'materialized view'
                    ELSE c.relkind::text
                END AS type,
                pg_get_userbyid(c.relowner) AS owner
            FROM pg_class c
            INNER JOIN pg_namespace n ON n.oid = c.relnamespace
            WHERE c.relname = @TableName
              AND (n.nspname = @TableSchema OR @TableSchema IS NULL)
              AND c.relkind IN ('r', 'p')";

        const string ColumnsQuery = @"
            SELECT
                a.attname AS name,
                format_type(a.atttypid, a.atttypmod) AS type,
                CASE
                    WHEN a.atttypmod > 0 AND t.typname IN ('varchar', 'bpchar')
                    THEN a.atttypmod - 4
                    ELSE NULL
                END AS length,
                CASE
                    WHEN t.typname IN ('numeric', 'decimal') AND a.atttypmod >= 0
                    THEN ((a.atttypmod - 4) >> 16) & 65535
                    ELSE NULL
                END AS precision,
                CASE
                    WHEN t.typname IN ('numeric', 'decimal') AND a.atttypmod >= 0
                    THEN (a.atttypmod - 4) & 65535
                    ELSE NULL
                END AS scale,
                NOT a.attnotnull AS nullable,
                col_description(a.attrelid, a.attnum) AS description
            FROM pg_attribute a
            INNER JOIN pg_class c ON a.attrelid = c.oid
            INNER JOIN pg_namespace n ON c.relnamespace = n.oid
            INNER JOIN pg_type t ON a.atttypid = t.oid
            WHERE c.relname = @TableName
              AND (n.nspname = @TableSchema OR @TableSchema IS NULL)
              AND a.attnum > 0
              AND NOT a.attisdropped
              AND c.relkind IN ('r', 'p')
            ORDER BY a.attnum";

        const string IndexesQuery = @"
            SELECT
                i.relname AS name,
                am.amname AS type,
                obj_description(ix.indexrelid, 'pg_class') AS description,
                (
                    SELECT string_agg(att.attname, ',' ORDER BY array_position(ix.indkey, att.attnum))
                    FROM pg_attribute att
                    WHERE att.attrelid = t.oid
                      AND att.attnum = ANY(ix.indkey)
                      AND att.attnum > 0
                      AND NOT att.attisdropped
                ) AS keys
            FROM pg_index ix
            INNER JOIN pg_class t ON t.oid = ix.indrelid
            INNER JOIN pg_class i ON i.oid = ix.indexrelid
            INNER JOIN pg_namespace n ON t.relnamespace = n.oid
            INNER JOIN pg_am am ON i.relam = am.oid
            WHERE t.relname = @TableName
              AND (n.nspname = @TableSchema OR @TableSchema IS NULL)
              AND NOT ix.indisprimary
              AND NOT ix.indisunique";

        const string ConstraintsQuery = @"
            SELECT
                con.conname AS name,
                CASE con.contype
                    WHEN 'p' THEN 'PRIMARY KEY'
                    WHEN 'u' THEN 'UNIQUE'
                    ELSE con.contype::text
                END AS type,
                (
                    SELECT string_agg(att.attname, ',' ORDER BY u.ord)
                    FROM unnest(con.conkey) WITH ORDINALITY AS u(attnum, ord)
                    INNER JOIN pg_attribute att ON att.attrelid = con.conrelid AND att.attnum = u.attnum
                ) AS keys
            FROM pg_constraint con
            INNER JOIN pg_class t ON t.oid = con.conrelid
            INNER JOIN pg_namespace n ON t.relnamespace = n.oid
            WHERE t.relname = @TableName
              AND (n.nspname = @TableSchema OR @TableSchema IS NULL)
              AND con.contype IN ('p', 'u')";

        const string ForeignKeyInformation = @"
            SELECT
                con.conname AS name,
                n.nspname AS schema,
                t.relname AS table_name,
                (
                    SELECT string_agg(att.attname, ', ' ORDER BY u.ord)
                    FROM unnest(con.conkey) WITH ORDINALITY AS u(attnum, ord)
                    INNER JOIN pg_attribute att ON att.attrelid = con.conrelid AND att.attnum = u.attnum
                ) AS column_names,
                fn.nspname AS referenced_schema,
                ft.relname AS referenced_table,
                (
                    SELECT string_agg(att.attname, ', ' ORDER BY u.ord)
                    FROM unnest(con.confkey) WITH ORDINALITY AS u(attnum, ord)
                    INNER JOIN pg_attribute att ON att.attrelid = con.confrelid AND att.attnum = u.attnum
                ) AS referenced_column_names
            FROM pg_constraint con
            INNER JOIN pg_class t ON t.oid = con.conrelid
            INNER JOIN pg_namespace n ON n.oid = t.relnamespace
            INNER JOIN pg_class ft ON ft.oid = con.confrelid
            INNER JOIN pg_namespace fn ON fn.oid = ft.relnamespace
            WHERE con.contype = 'f'
              AND t.relname = @TableName
              AND (n.nspname = @TableSchema OR @TableSchema IS NULL)";
    }
}
