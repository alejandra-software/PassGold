using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GoldeenRide.Models;
using Microsoft.Maui.Storage;
using Newtonsoft.Json;
using Supabase;
using Supabase.Gotrue;
using Session = Supabase.Gotrue.Session;
using SupabaseClient = Supabase.Client;
using GotrueUser = Supabase.Gotrue.User;

namespace GoldeenRide.Services;

public class SupabaseService
{
    private const string SupabaseUrl = "";
    private const string SupabaseAnonKey = "";

    private static SupabaseService? _instance;
    private SupabaseClient? _supabaseClient;
    private Session? _currentSession;
    private bool _isInitialized = false;

#if ANDROID
    public static TaskCompletionSource<(string? Token, string Error)>? GoogleSignInTcs;
#endif

    public static SupabaseService Instance => _instance ??= new SupabaseService();
    public Session? CurrentSession => _currentSession;
    public bool IsAuthenticated => _currentSession != null;

    public SupabaseService() { }

    private async Task EnsureInitializedAsync()
    {
        if (_isInitialized) return;
        try
        {
            var options = new SupabaseOptions { AutoRefreshToken = true, AutoConnectRealtime = false };
            _supabaseClient = new SupabaseClient(SupabaseUrl, SupabaseAnonKey, options);
            await _supabaseClient.InitializeAsync();
            _isInitialized = true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Error conectando Supabase: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> IsSessionActiveAsync()
    {
        try
        {
            await EnsureInitializedAsync();
            string? savedSessionJson = await SecureStorage.Default.GetAsync("supabase_session");

            if (!string.IsNullOrEmpty(savedSessionJson))
            {
                var session = JsonConvert.DeserializeObject<Session>(savedSessionJson);
                if (session != null && !string.IsNullOrEmpty(session.AccessToken) && !string.IsNullOrEmpty(session.RefreshToken))
                {
                    _currentSession = await _supabaseClient!.Auth.SetSession(session.AccessToken!, session.RefreshToken!);
                    return true;
                }
            }
            return false;
        }
        catch { return false; }
    }

    public async Task<(bool Success, string Message)> RegisterAsync(string email, string password, string nombre, string rol, string fotoPerfil)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                return (false, "Error: El correo o contraseña se perdieron en la memoria.");

            await EnsureInitializedAsync();
            if (_supabaseClient == null) return (false, "No se pudo conectar");

            var options = new SignUpOptions { Data = new Dictionary<string, object> { { "nombre", nombre }, { "rol", rol }, { "foto_perfil", fotoPerfil } } };
            await _supabaseClient.Auth.SignUp(email.Trim(), password, options);
            return (true, "Registro exitoso.");
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public async Task<(bool Success, string Message, Session? Session)> LoginAsync(string email, string password)
    {
        try
        {
            await EnsureInitializedAsync();
            if (_supabaseClient == null) return (false, "Error de conexión", null);
            var session = await _supabaseClient.Auth.SignInWithPassword(email.Trim(), password);
            _currentSession = session;

            string sessionJson = JsonConvert.SerializeObject(session);
            await SecureStorage.Default.SetAsync("supabase_session", sessionJson);

            return (true, "Sesión iniciada", session);
        }
        catch (Exception) { return (false, "Correo o contraseña incorrectos.", null); }
    }

    public async Task<(bool Success, string Message, Session? Session)> LoginWithGoogleNativeAsync()
    {
#if ANDROID
        try
        {
            await EnsureInitializedAsync();

            string webClientId = "851890940381-7g35rn7qv9m7b2r8sshnaugjhkt7vtc3.apps.googleusercontent.com";

            var context = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
            if (context == null) return (false, "No se pudo obtener el contexto de Android.", null);

            var gso = new Android.Gms.Auth.Api.SignIn.GoogleSignInOptions.Builder(Android.Gms.Auth.Api.SignIn.GoogleSignInOptions.DefaultSignIn)
                .RequestIdToken(webClientId)
                .RequestEmail()
                .Build();

            var googleSignInClient = Android.Gms.Auth.Api.SignIn.GoogleSignIn.GetClient(context, gso);
            googleSignInClient.SignOut();

            GoogleSignInTcs = new TaskCompletionSource<(string? Token, string Error)>();

            context.StartActivityForResult(googleSignInClient.SignInIntent, 9001);

            var result = await GoogleSignInTcs.Task;

            if (!string.IsNullOrEmpty(result.Token))
            {
                var session = await _supabaseClient!.Auth.SignInWithIdToken(Supabase.Gotrue.Constants.Provider.Google, result.Token);

                if (session != null)
                {
                    _currentSession = session;
                    await SecureStorage.Default.SetAsync("supabase_session", JsonConvert.SerializeObject(session));
                    return (true, "Sesión iniciada con Google", session);
                }
                return (false, "No se pudo crear la sesión en Supabase.", null);
            }
            else
            {
                if (result.Error.Contains("16") || result.Error.Contains("CANCELED"))
                    return (false, "Operación cancelada.", null);

                return (false, $"Falló la conexión con Google -> {result.Error}", null);
            }
        }
        catch (Exception ex)
        {
            return (false, $"Error interno: {ex.Message}", null);
        }
#else
        return (false, "El login nativo solo está configurado para Android.", null);
#endif
    }

    public async Task<bool> LogoutAsync()
    {
        try
        {
            await EnsureInitializedAsync();
            if (_supabaseClient == null) return false;
            await _supabaseClient.Auth.SignOut();
            _currentSession = null;
            SecureStorage.Default.Remove("supabase_session");
            return true;
        }
        catch { return false; }
    }

    public GotrueUser? GetCurrentUser() => _supabaseClient?.Auth.CurrentUser;

    public async Task<Usuario?> GetUserDataAsync(string userId)
    {
        try
        {
            await EnsureInitializedAsync();
            if (_supabaseClient == null || string.IsNullOrEmpty(userId)) return null;
            // 🔥 FIX: Eliminamos Single() y usamos Get().Models.FirstOrDefault() para evitar crasheos silenciosos
            var response = await _supabaseClient.From<Usuario>().Filter("id", Supabase.Postgrest.Constants.Operator.Equals, userId).Get();
            return response.Models?.FirstOrDefault();
        }
        catch { return null; }
    }

    public async Task<bool> SaveUserDataAsync(string userId, string nombre, string rol, string? fotoPerfil = null)
    {
        try
        {
            await EnsureInitializedAsync();
            if (_supabaseClient == null || string.IsNullOrEmpty(userId)) return false;

            try
            {
                var response = await _supabaseClient.From<Usuario>().Filter("id", Supabase.Postgrest.Constants.Operator.Equals, userId).Get();
                var usuario = response.Models?.FirstOrDefault();

                if (usuario != null)
                {
                    usuario.Nombre = nombre;
                    usuario.Rol = rol;
                    if (!string.IsNullOrEmpty(fotoPerfil)) usuario.FotoPerfil = fotoPerfil;

                    await usuario.Update<Usuario>();
                    return true;
                }
            }
            catch { }

            var currentUser = _supabaseClient.Auth.CurrentUser;
            var nuevoUsuario = new Usuario
            {
                Id = userId,
                Email = currentUser?.Email ?? "",
                Nombre = nombre,
                Rol = rol,
                FotoPerfil = fotoPerfil
            };

            await _supabaseClient.From<Usuario>().Insert(nuevoUsuario);
            return true;
        }
        catch { return false; }
    }

    public async Task<List<Usuario>> GetEmployeesByBossAsync(string bossId)
    {
        try
        {
            await EnsureInitializedAsync();
            if (_supabaseClient == null || string.IsNullOrEmpty(bossId)) return new List<Usuario>();
            var response = await _supabaseClient.From<Usuario>().Where(u => u.IdJefe == bossId).Get();
            return response.Models ?? new List<Usuario>();
        }
        catch { return new List<Usuario>(); }
    }

    private const string FleetCodeChars = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
    private static readonly Random _fleetCodeRandom = new();

    public async Task<string?> GetOrCreateFleetCodeAsync(string jefeId)
    {
        try
        {
            await EnsureInitializedAsync();
            var response = await _supabaseClient!.From<Usuario>().Filter("id", Supabase.Postgrest.Constants.Operator.Equals, jefeId).Get();
            var usuario = response.Models?.FirstOrDefault();

            if (usuario == null) return null;

            if (!string.IsNullOrWhiteSpace(usuario.CodigoFlota))
                return usuario.CodigoFlota;

            string nuevoCodigo = "";
            bool esUnico = false;
            int intentos = 0;

            while (!esUnico && intentos < 15)
            {
                nuevoCodigo = GenerarCodigoAleatorio(6);
                esUnico = !(await ExisteCodigoFlotaAsync(nuevoCodigo));
                intentos++;
            }

            if (!esUnico) return null;

            usuario.CodigoFlota = nuevoCodigo;
            await usuario.Update<Usuario>();
            return nuevoCodigo;
        }
        catch { return null; }
    }

    public async Task<string?> RegenerateFleetCodeAsync(string jefeId)
    {
        try
        {
            await EnsureInitializedAsync();
            var response = await _supabaseClient!.From<Usuario>().Filter("id", Supabase.Postgrest.Constants.Operator.Equals, jefeId).Get();
            var usuario = response.Models?.FirstOrDefault();

            if (usuario == null) return null;

            string nuevoCodigo = "";
            bool esUnico = false;
            int intentos = 0;

            while (!esUnico && intentos < 15)
            {
                nuevoCodigo = GenerarCodigoAleatorio(6);
                esUnico = !(await ExisteCodigoFlotaAsync(nuevoCodigo));
                intentos++;
            }

            if (!esUnico) return null;

            usuario.CodigoFlota = nuevoCodigo;
            await usuario.Update<Usuario>();

            return nuevoCodigo;
        }
        catch { return null; }
    }

    private string GenerarCodigoAleatorio(int longitud)
    {
        var chars = new char[longitud];
        for (int i = 0; i < longitud; i++)
            chars[i] = FleetCodeChars[_fleetCodeRandom.Next(FleetCodeChars.Length)];
        return new string(chars);
    }

    private async Task<bool> ExisteCodigoFlotaAsync(string codigo)
    {
        try
        {
            var response = await _supabaseClient!.From<Usuario>().Filter("codigo_flota", Supabase.Postgrest.Constants.Operator.Equals, codigo).Get();
            return response.Models != null && response.Models.Count > 0;
        }
        catch { return false; }
    }

    public async Task<(bool Success, string Message)> JoinFleetByCodeAsync(string codigoFlota, string employeeId)
    {
        try
        {
            await EnsureInitializedAsync();
            if (_supabaseClient == null) return (false, "Error de conexión.");

            string codigoNormalizado = codigoFlota.Trim().ToUpperInvariant();
            var bossResponse = await _supabaseClient.From<Usuario>().Filter("codigo_flota", Supabase.Postgrest.Constants.Operator.Equals, codigoNormalizado).Get();
            var boss = bossResponse.Models?.FirstOrDefault();

            if (boss == null || !boss.Rol.ToLower().Contains("jefe"))
                return (false, "El código ingresado no es válido.");

            try
            {
                await _supabaseClient.From<Usuario>()
                    .Where(u => u.Id == employeeId)
                    .Set(u => u.IdJefe, boss.Id)
                    .Update();
            }
            catch (Exception updateEx) when (updateEx.Message.Contains("Sequence contains no elements")) { }

            return (true, $"¡Te has unido con éxito a la flota de {boss.Nombre}!");
        }
        catch (Exception ex)
        {
            return (false, $"Error al unirse: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> LinkEmployeeByEmailAsync(string emailEmpleado, string idJefe)
    {
        try
        {
            await EnsureInitializedAsync();
            if (_supabaseClient == null) return (false, "Error de conexión");
            var response = await _supabaseClient.From<Usuario>().Where(u => u.Email == emailEmpleado.Trim()).Get();
            var empleado = response.Models?.FirstOrDefault();
            if (empleado == null) return (false, "No se encontró el correo.");
            if (!empleado.Rol.Contains("empleado")) return (false, "Este usuario no es Chofer Empleado.");
            if (!string.IsNullOrEmpty(empleado.IdJefe)) return (false, "Ya trabaja para otro jefe.");
            empleado.IdJefe = idJefe;
            await empleado.Update<Usuario>();
            return (true, $"¡Vinculado con éxito!");
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public async Task<List<Vehiculo>> GetVehiclesByOwnerAsync(string ownerId)
    {
        try
        {
            await EnsureInitializedAsync();
            if (_supabaseClient == null || string.IsNullOrEmpty(ownerId)) return new List<Vehiculo>();
            var response = await _supabaseClient.From<Vehiculo>().Where(v => v.IdPropietario == ownerId).Get();
            return response.Models ?? new List<Vehiculo>();
        }
        catch { return new List<Vehiculo>(); }
    }

    public async Task<Vehiculo?> GetVehiculoByIdAsync(string idVehiculo)
    {
        try
        {
            await EnsureInitializedAsync();
            var resp = await _supabaseClient!.From<Vehiculo>().Filter("id", Supabase.Postgrest.Constants.Operator.Equals, idVehiculo).Get();
            return resp.Models?.FirstOrDefault();
        }
        catch { return null; }
    }

    public async Task<bool> AddVehicleAsync(Vehiculo vehiculo)
    {
        try
        {
            await EnsureInitializedAsync();
            if (_supabaseClient == null) return false;
            await _supabaseClient.From<Vehiculo>().Insert(vehiculo);
            return true;
        }
        catch { return false; }
    }

    public async Task<bool> DeactivateVehiculoAsync(string idVehiculo)
    {
        try
        {
            await EnsureInitializedAsync();
            var vehiculo = await GetVehiculoByIdAsync(idVehiculo);
            if (vehiculo != null)
            {
                vehiculo.Estado = "inactivo";
                await vehiculo.Update<Vehiculo>();
                return true;
            }
            return false;
        }
        catch { return false; }
    }

    public async Task<bool> ReactivateVehiculoAsync(string idVehiculo)
    {
        try
        {
            await EnsureInitializedAsync();
            var vehiculo = await GetVehiculoByIdAsync(idVehiculo);
            if (vehiculo != null)
            {
                vehiculo.Estado = "activo";
                await vehiculo.Update<Vehiculo>();
                return true;
            }
            return false;
        }
        catch { return false; }
    }

    public async Task<bool> RemoveEmployeeFromFleetAsync(string employeeId)
    {
        try
        {
            await EnsureInitializedAsync();
            var employee = await GetUserDataAsync(employeeId);
            if (employee != null)
            {
                employee.IdJefe = null;
                await employee.Update<Usuario>();
                return true;
            }
            return false;
        }
        catch { return false; }
    }

    public async Task<bool> CreateTripAsync(Viaje viaje)
    {
        try
        {
            await EnsureInitializedAsync();
            if (_supabaseClient == null) return false;
            await _supabaseClient.From<Viaje>().Insert(viaje);
            return true;
        }
        catch { return false; }
    }

    public async Task<(Viaje? Viaje, string Error)> CreateTripAndReturnAsync(Viaje viaje)
    {
        try
        {
            await EnsureInitializedAsync();
            var resp = await _supabaseClient!.From<Viaje>().Insert(viaje);
            return (resp.Models?.FirstOrDefault(), "");
        }
        catch (Exception ex) { return (null, ex.Message); }
    }

    public async Task<(bool Success, string Error)> CreateAssignmentsBulkAsync(List<Asignacion> asignaciones)
    {
        try
        {
            await EnsureInitializedAsync();
            await _supabaseClient!.From<Asignacion>().Insert(asignaciones);
            return (true, "");
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public async Task<List<Viaje>> GetAllActiveTripsAsync(List<string>? idsCreadoresFlota = null)
    {
        try
        {
            await EnsureInitializedAsync();
            if (_supabaseClient == null) return new List<Viaje>();

            var estados = new List<string> { "activo", "programado" };
            var query = _supabaseClient.From<Viaje>().Filter("estado", Supabase.Postgrest.Constants.Operator.In, estados);

            if (idsCreadoresFlota != null && idsCreadoresFlota.Count > 0)
                query = query.Filter("id_creador", Supabase.Postgrest.Constants.Operator.In, idsCreadoresFlota);

            var response = await query.Get();
            var modelos = response.Models ?? new List<Viaje>();

            foreach (var v in modelos)
            {
                // 🔥 FIX DOBLE RESTA: Newtonsoft ya aplicó la hora local de El Salvador, 
                // pero a veces pierde la etiqueta. Le ponemos la etiqueta "Local" para 
                // que C# no intente restarle 6 horas de nuevo.
                if (v.HoraSalida.Kind == DateTimeKind.Unspecified)
                    v.HoraSalida = DateTime.SpecifyKind(v.HoraSalida, DateTimeKind.Local);
                else if (v.HoraSalida.Kind == DateTimeKind.Utc)
                    v.HoraSalida = v.HoraSalida.ToLocalTime();
            }
            return modelos;
        }
        catch { return new List<Viaje>(); }
    }

    public async Task<List<Asignacion>> GetAsignacionesPorRangoAsync(DateTime fechaInicio, DateTime fechaFin, List<string>? idsChoferesFlota = null)
    {
        try
        {
            await EnsureInitializedAsync();
            string start = fechaInicio.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ", System.Globalization.CultureInfo.InvariantCulture);
            string end = fechaFin.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ", System.Globalization.CultureInfo.InvariantCulture);

            var query = _supabaseClient!.From<Asignacion>()
                .Filter("fecha", Supabase.Postgrest.Constants.Operator.GreaterThanOrEqual, start)
                .Filter("fecha", Supabase.Postgrest.Constants.Operator.LessThanOrEqual, end);

            if (idsChoferesFlota != null && idsChoferesFlota.Count > 0)
                query = query.Filter("id_chofer", Supabase.Postgrest.Constants.Operator.In, idsChoferesFlota);

            var resp = await query.Get();
            var modelos = resp.Models ?? new List<Asignacion>();

            foreach (var asig in modelos)
            {
                // 🔥 FIX DOBLE RESTA: Misma lógica de protección
                if (asig.Fecha.Kind == DateTimeKind.Unspecified)
                    asig.Fecha = DateTime.SpecifyKind(asig.Fecha, DateTimeKind.Local);
                else if (asig.Fecha.Kind == DateTimeKind.Utc)
                    asig.Fecha = asig.Fecha.ToLocalTime();
            }
            return modelos;
        }
        catch { return new List<Asignacion>(); }
    }

    public async Task<Asignacion?> GetAsignacionByIdAsync(string idAsignacion)
    {
        try
        {
            await EnsureInitializedAsync();
            var resp = await _supabaseClient!.From<Asignacion>().Filter("id", Supabase.Postgrest.Constants.Operator.Equals, idAsignacion).Get();
            return resp.Models?.FirstOrDefault();
        }
        catch { return null; }
    }

    public async Task<bool> CreateReservaAsync(Reserva reserva)
    {
        try
        {
            await EnsureInitializedAsync();
            if (_supabaseClient == null) return false;
            await _supabaseClient.From<Reserva>().Insert(reserva);
            return true;
        }
        catch { return false; }
    }

    public async Task<List<Reserva>> GetReservasByAsignacionAsync(string idAsignacion)
    {
        try
        {
            await EnsureInitializedAsync();
            var resp = await _supabaseClient!.From<Reserva>().Filter("id_asignacion", Supabase.Postgrest.Constants.Operator.Equals, idAsignacion).Get();
            return resp.Models ?? new List<Reserva>();
        }
        catch { return new List<Reserva>(); }
    }

    public async Task<bool> UpdateAsignacionIndividualAsync(string idAsignacion, string nuevoIdChofer, string nuevoIdVehiculo, DateTime nuevaFechaHora)
    {
        try
        {
            await EnsureInitializedAsync();
            var asig = await GetAsignacionByIdAsync(idAsignacion);
            if (asig != null)
            {
                asig.IdChofer = nuevoIdChofer;
                asig.IdVehiculo = nuevoIdVehiculo;
                asig.Fecha = nuevaFechaHora.ToUniversalTime();
                await asig.Update<Asignacion>();
                return true;
            }
            return false;
        }
        catch { return false; }
    }

    public async Task<bool> UpdateRutaCompletaAsync(string idViaje, DateTime fechaDesde, string nuevoIdChofer, string nuevoIdVehiculo, TimeSpan nuevaHora)
    {
        try
        {
            await EnsureInitializedAsync();
            string fechaStr = fechaDesde.ToUniversalTime().ToString("o");
            var response = await _supabaseClient!.From<Asignacion>()
                .Filter("id_viaje", Supabase.Postgrest.Constants.Operator.Equals, idViaje)
                .Filter("fecha", Supabase.Postgrest.Constants.Operator.GreaterThanOrEqual, fechaStr)
                .Get();

            if (response.Models != null && response.Models.Count > 0)
            {
                foreach (var asig in response.Models)
                {
                    asig.IdChofer = nuevoIdChofer;
                    asig.IdVehiculo = nuevoIdVehiculo;
                    DateTime localDate = asig.Fecha.ToLocalTime().Date;
                    asig.Fecha = localDate.Add(nuevaHora).ToUniversalTime();
                }
                await _supabaseClient.From<Asignacion>().Upsert(response.Models);
            }

            var viajeResp = await _supabaseClient.From<Viaje>().Filter("id", Supabase.Postgrest.Constants.Operator.Equals, idViaje).Get();
            var viajeActual = viajeResp.Models?.FirstOrDefault();
            if (viajeActual != null)
            {
                DateTime templateLocalDate = viajeActual.HoraSalida.ToLocalTime().Date;
                viajeActual.HoraSalida = templateLocalDate.Add(nuevaHora).ToUniversalTime();
                await viajeActual.Update<Viaje>();
            }
            return true;
        }
        catch { return false; }
    }

    public async Task<bool> DeleteAsignacionIndividualAsync(string idAsignacion)
    {
        try
        {
            await EnsureInitializedAsync();
            await _supabaseClient!.From<Asignacion>().Where(a => a.Id == idAsignacion).Delete();
            return true;
        }
        catch { return false; }
    }

    public async Task<bool> DeleteRutaCompletaAsync(string idViaje)
    {
        try
        {
            await EnsureInitializedAsync();
            await _supabaseClient!.From<Viaje>().Where(v => v.Id == idViaje).Delete();
            return true;
        }
        catch { return false; }
    }
}