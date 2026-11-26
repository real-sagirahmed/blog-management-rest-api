using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BlogManagement.API.Helpers;

internal static class DbExceptionClassifier
{
    public static bool IsUniqueViolation(DbUpdateException exception)
    {
        if (exception.InnerException is SqlException sqlEx)
        {
            return sqlEx.Number is 2601 or 2627;
        }

        // Support SQLite unique constraint checks in tests without coupling the API project to Sqlite.
        return TryGetSqliteErrorCodes(exception.InnerException, out var sqliteCode, out var extendedCode)
               && (extendedCode == 2067 || sqliteCode == 19 && exception.InnerException!.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsConstraintConflict(DbUpdateException exception)
    {
        if (exception.InnerException is SqlException sqlEx)
        {
            // 2601 / 2627: unique index / primary key violation; 547: foreign key constraint violation.
            return sqlEx.Number is 2601 or 2627 or 547;
        }

        return TryGetSqliteErrorCodes(exception.InnerException, out var sqliteCode, out _)
               && sqliteCode == 19;
    }

    private static bool TryGetSqliteErrorCodes(Exception? inner, out int sqliteErrorCode, out int extendedErrorCode)
    {
        sqliteErrorCode = 0;
        extendedErrorCode = 0;

        if (inner?.GetType().FullName != "Microsoft.Data.Sqlite.SqliteException")
        {
            return false;
        }

        var type = inner.GetType();
        if (type.GetProperty("SqliteErrorCode")?.GetValue(inner) is int code)
        {
            sqliteErrorCode = code;
        }

        if (type.GetProperty("SqliteExtendedErrorCode")?.GetValue(inner) is int extCode)
        {
            extendedErrorCode = extCode;
        }

        return true;
    }
}
