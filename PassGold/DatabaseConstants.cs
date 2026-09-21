using System.IO;

namespace PassGold;

public static class DatabaseConstants
{
    public const string DatabaseFilename = "PassGoldLocal.db3";

    public const SQLite.SQLiteOpenFlags Flags =
        // Abre la base de datos en modo lectura/escritura
        SQLite.SQLiteOpenFlags.ReadWrite |
        // Crea la base de datos si no existe
        SQLite.SQLiteOpenFlags.Create |
        // Habilita el acceso multi-hilo para evitar bloqueos
        SQLite.SQLiteOpenFlags.SharedCache;

    public static string DatabasePath =>
        Path.Combine(FileSystem.AppDataDirectory, DatabaseFilename);
}
