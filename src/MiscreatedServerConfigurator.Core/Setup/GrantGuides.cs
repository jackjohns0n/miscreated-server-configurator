using Microsoft.Data.Sqlite;

namespace MiscreatedServerConfigurator.Core.Setup;

/// <summary>What a grant-guides change did.</summary>
/// <param name="Changed">True when the database was modified.</param>
/// <param name="Message">A sentence for the setup window.</param>
/// <param name="BackupPath">The backup file, when one was written.</param>
public readonly record struct GrantGuidesResult(bool Changed, string Message, string? BackupPath);

/// <summary>
/// Gives every account all guides by a SQLite trigger, or removes that trigger.
/// </summary>
/// <remarks>
/// The trigger runs <c>UPDATE ServerAccountData SET Guide00 = '-1', Guide01 = '-1'</c> with no WHERE clause.
/// That updates every row on purpose. The quotes around <c>-1</c> match the requested statement.
/// Whether those columns are text or integers in a real <c>miscreated.db</c> was not checked. SQLite will coerce a numeric column.
/// The database file is created by the server on its first start. Until then there is nothing to modify.
/// </remarks>
public static class GrantGuides
{
    /// <summary>Trigger name. Dropping it turns the feature off.</summary>
    public const string TriggerName = "GrantAllGuides";

    /// <summary>Creates the trigger. It is not wrapped in <c>IF NOT EXISTS</c> because the caller drops it first.</summary>
    public const string CreateTriggerSql =
        "CREATE TRIGGER GrantAllGuides AFTER UPDATE ON Characters BEGIN UPDATE ServerAccountData SET Guide00 = '-1', Guide01 = '-1'; END;";

    /// <summary>One-time update of rows that already exist when the trigger is added.</summary>
    public const string UpdateExistingSql = "UPDATE ServerAccountData SET Guide00 = '-1', Guide01 = '-1';";

    /// <summary>Removes the trigger. Existing guide values are left as they are.</summary>
    public const string DropTriggerSql = "DROP TRIGGER IF EXISTS GrantAllGuides;";

    /// <summary>The usual database file name inside a server folder.</summary>
    public const string DatabaseFileName = "miscreated.db";

    /// <summary>Shown when the server has not created the database yet.</summary>
    public const string DatabaseMissingMessage =
        "miscreated.db is not in this server folder yet. The server creates it the first time it starts. Start the server with the batch script, stop it, then apply grant-all-guides.";

    /// <summary>Path of <c>miscreated.db</c> in a server folder.</summary>
    /// <param name="serverDirectory">The server folder.</param>
    /// <returns>The database path.</returns>
    public static string DatabasePath(string serverDirectory)
    {
        return Path.Combine(serverDirectory, DatabaseFileName);
    }

    /// <summary>True when the trigger is already in the database.</summary>
    /// <param name="databasePath">The database file.</param>
    /// <returns><see langword="false"/> when the file is missing or the trigger is absent.</returns>
    public static bool TriggerExists(string databasePath)
    {
        if (!File.Exists(databasePath))
        {
            return false;
        }

        using var connection = Open(databasePath);
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM sqlite_master WHERE type = 'trigger' AND name = $name LIMIT 1;";
        command.Parameters.AddWithValue("$name", TriggerName);
        return command.ExecuteScalar() is not null;
    }

    /// <summary>Backs up the database, replaces the trigger, and updates every current row.</summary>
    /// <param name="databasePath">The database file.</param>
    /// <returns>What happened.</returns>
    public static GrantGuidesResult Apply(string databasePath)
    {
        if (!File.Exists(databasePath))
        {
            return new GrantGuidesResult(false, DatabaseMissingMessage, null);
        }

        var backup = Backup(databasePath);
        try
        {
            using var connection = Open(databasePath);
            using var transaction = connection.BeginTransaction();
            Execute(connection, DropTriggerSql);
            Execute(connection, CreateTriggerSql);
            Execute(connection, UpdateExistingSql);
            transaction.Commit();
            return new GrantGuidesResult(true, "Grant-all-guides is on. A backup was written to " + backup + ".", backup);
        }
        catch (SqliteException ex)
        {
            return new GrantGuidesResult(false, "The database was not changed after the backup. " + ex.Message, backup);
        }
    }

    /// <summary>Backs up the database and drops the trigger. Guide values already stored are not rewritten.</summary>
    /// <param name="databasePath">The database file.</param>
    /// <returns>What happened.</returns>
    public static GrantGuidesResult Remove(string databasePath)
    {
        if (!File.Exists(databasePath))
        {
            return new GrantGuidesResult(false, DatabaseMissingMessage, null);
        }

        var backup = Backup(databasePath);
        try
        {
            using var connection = Open(databasePath);
            Execute(connection, DropTriggerSql);
            return new GrantGuidesResult(true, "Grant-all-guides is off. Existing guide values were left as they are. A backup was written to " + backup + ".", backup);
        }
        catch (SqliteException ex)
        {
            return new GrantGuidesResult(false, "The trigger was not dropped. " + ex.Message, backup);
        }
    }

    private static string Backup(string databasePath)
    {
        var backup = databasePath + ".bak-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff", System.Globalization.CultureInfo.InvariantCulture);
        File.Copy(databasePath, backup, overwrite: false);
        return backup;
    }

    private static SqliteConnection Open(string databasePath)
    {
        var connection = new SqliteConnection("Data Source=" + databasePath);
        connection.Open();
        return connection;
    }

    private static void Execute(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}
