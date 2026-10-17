using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace TaskManager.Infrastructure.Persistence;

public static class DbExceptionExtensions
{
    public static bool IsUniqueViolation(this DbUpdateException ex, string constraintName) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg
        && pg.ConstraintName == constraintName;
}