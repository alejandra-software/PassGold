using SQLite;
using System.Threading.Tasks;
using PassGold.Models.Local;
using System;
using Microsoft.Maui.Storage;
using System.Threading; // ?? Necesario para el Sem�foro

namespace PassGold.Services;

public class LocalDatabaseService
{
    private static LocalDatabaseService? _instance;
    public static LocalDatabaseService Instance => _instance ??= new LocalDatabaseService();

    //  ANTICRASH: 
    // Al ser "static", TODA la aplicacion compartira esta nica conexion y este unico semaforo, 
    // sin importar cu�ntas pantallas la llamen al mismo tiempo.
    private static SQLiteAsyncConnection? _conexion;
    private static readonly SemaphoreSlim _semaforo = new SemaphoreSlim(1, 1);

    private string DbPath => System.IO.Path.Combine(FileSystem.AppDataDirectory, "PassGoldLocal.db3");

    public LocalDatabaseService() { }

    public async Task IniciarConexionAsync()
    {
        // Si ya est� conectada, pasamos de largo instant�neamente
        if (_conexion != null) return;

        // ?? EL SEM�FORO: Si dos pantallas llegan aqu� a la vez, una espera a que la otra termine.
        await _semaforo.WaitAsync();
        try
        {
            // Doble verificaci�n por si otra pantalla ya hizo el trabajo mientras esta esperaba
            if (_conexion != null) return;

            _conexion = new SQLiteAsyncConnection(DbPath);
            await _conexion.CreateTableAsync<UsuarioLocal>();
            await _conexion.CreateTableAsync<CacheLocal>();
        }
        finally
        {
            // Liberamos el sem�foro para que pase el siguiente en la fila
            _semaforo.Release();
        }
    }

    public async Task<bool> GuardarSesionActivaAsync(UsuarioLocal usuario)
    {
        try
        {
            await IniciarConexionAsync();
            await _conexion!.DeleteAllAsync<UsuarioLocal>();
            await _conexion.InsertAsync(usuario);
            return true;
        }
        catch { return false; }
    }

    public async Task<UsuarioLocal?> ObtenerSesionActivaAsync()
    {
        try
        {
            await IniciarConexionAsync();
            return await _conexion!.Table<UsuarioLocal>().FirstOrDefaultAsync();
        }
        catch { return null; }
    }

    public async Task<bool> CerrarSesionLocalAsync()
    {
        try
        {
            await IniciarConexionAsync();
            await _conexion!.DeleteAllAsync<UsuarioLocal>();
            return true;
        }
        catch { return false; }
    }

    // ??? METODOS DE CACHE OFFLINE 

    public async Task GuardarCacheAsync(string clave, string json)
    {
        try
        {
            await IniciarConexionAsync();
            var cache = new CacheLocal { Clave = clave, DatosJson = json };
            await _conexion!.InsertOrReplaceAsync(cache);
        }
        catch { }
    }

    public async Task<string?> LeerCacheAsync(string clave)
    {
        try
        {
            await IniciarConexionAsync();
            var cache = await _conexion!.Table<CacheLocal>().FirstOrDefaultAsync(c => c.Clave == clave);
            return cache?.DatosJson;
        }
        catch { return null; }
    }
}
