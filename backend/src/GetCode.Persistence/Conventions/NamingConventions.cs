using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace GetCode.Persistence.Conventions;

/// <summary>
/// M00-006 database naming policy (see docs/architecture/DATABASE.md):
/// - tables/columns: snake_case (tables additionally pluralised by appending "s");
/// - primary keys: pk_{table}; foreign keys: fk_{table}_{principal_table};
/// - indexes: ix_{table}__{columns}; primary keys double as named constraints.
/// Explicit configuration always wins: only identifiers left at EF defaults
/// (CLR member names) are rewritten, so per-entity mappings stay authoritative.
/// </summary>
internal static class NamingConventions
{
    public static void Apply(ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            var tableName = entity.GetTableName();
            if (tableName is null)
            {
                continue;
            }

            if (string.Equals(tableName, entity.ClrType.Name, StringComparison.Ordinal))
            {
                // Default table name straight from the CLR type: normalise it.
                entity.SetTableName(ToSnakeCase(tableName) + "s");
            }

            var effectiveTable = entity.GetTableName()!;

            foreach (var property in entity.GetProperties())
            {
                var columnName = property.GetColumnName(StoreObjectIdentifier.Table(effectiveTable, null));
                if (columnName is not null && string.Equals(columnName, property.Name, StringComparison.Ordinal))
                {
                    property.SetColumnName(ToSnakeCase(property.Name));
                }
            }

            foreach (var key in entity.GetKeys())
            {
                var defaultName = (key.IsPrimaryKey() ? "PK_" : "AK_") + tableName;
                if (key.GetName()?.Equals(defaultName, StringComparison.OrdinalIgnoreCase) != false)
                {
                    key.SetName((key.IsPrimaryKey() ? "pk_" : "ux_") + effectiveTable);
                }
            }

            foreach (var foreignKey in entity.GetForeignKeys())
            {
                var principalTable = foreignKey.PrincipalEntityType.GetTableName();
                if (principalTable is not null)
                {
                    foreignKey.SetConstraintName($"fk_{effectiveTable}_{principalTable}");
                }
            }

            foreach (var index in entity.GetIndexes())
            {
                // Leave explicitly named indexes alone.
                if (index.Name is { } existing && !existing.StartsWith("IX_", StringComparison.Ordinal))
                {
                    continue;
                }

                var columns = string.Join(
                    "_",
                    index.Properties.Select(p =>
                        ToSnakeCase(p.Name)));
                index.SetDatabaseName($"ix_{effectiveTable}__{columns}");
            }
        }
    }

    public static string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        var sb = new StringBuilder(name.Length + 8);
        for (var i = 0; i < name.Length; i++)
        {
            var current = name[i];
            if (char.IsUpper(current))
            {
                var previousLowercaseOrDigit = i > 0 && (char.IsLower(name[i - 1]) || char.IsDigit(name[i - 1]));
                var nextLowercase = i + 1 < name.Length && char.IsLower(name[i + 1]);
                if (i > 0 && (previousLowercaseOrDigit || nextLowercase))
                {
                    sb.Append('_');
                }

                sb.Append(char.ToLowerInvariant(current));
            }
            else
            {
                sb.Append(current);
            }
        }

        return sb.ToString();
    }
}
