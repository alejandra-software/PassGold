using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using PassGold.Models;
using PassGold.Helpers;
using Microsoft.Maui.Storage;
using Newtonsoft.Json;
using Supabase;
using Supabase.Gotrue;
using Session = Supabase.Gotrue.Session;
using SupabaseClient = Supabase.Client;
using GotrueUser = Supabase.Gotrue.User;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Authentication;

namespace PassGold.Services;

public class SupabaseService
{
    

    private static SupabaseService? _instance;
    private SupabaseClient? _supabaseClient;
    private Session? _currentSession;
    private bool _isInitialized = false;

    public static TaskCompletionSource<(string? Token, string Error)>? GoogleSignInTcs;

    public static SupabaseService Instance => _instance ??= new SupabaseService();
    public Session? CurrentSession => _currentSession;
    public bool IsAuthenticated => _currentSession != null;

    public SupabaseClient? Client => _supabaseClient;

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
                return (false, AppResources.Auth_CredentialsLost);

            await EnsureInitializedAsync();
            if (_supabaseClient == null) return (false, AppResources.Error_ConnectionFailed);

            var options = new SignUpOptions { Data = new Dictionary<string, object> { { "nombre", nombre }, { "rol", rol }, { "foto_perfil", fotoPerfil } } };
            await _supabaseClient.Auth.SignUp(email.Trim(), password, options);
            return (true, AppResources.Success_Registration);
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public async Task<(bool Success, string Message, Session? Session)> LoginAsync(string email, string password)
    {
        try
        {
            await EnsureInitializedAsync();
            if (_supabaseClient == null) return (false, AppResources.Error_Connection, null);
            var session = await _supabaseClient.Auth.SignInWithPassword(email.Trim(), password);
            _currentSession = session;

            string sessionJson = JsonConvert.SerializeObject(session);
            await SecureStorage.Default.SetAsync("supabase_session", sessionJson);

            return (true, AppResources.Success_Login, session);
        }
        catch (Exception) { return (false, AppResources.Error_InvalidCredentials, null); }
    }

    // MÉTODO PARA ANDROID (NATIVO)
    public async Task<(bool Success, string Message, Session? Session)> LoginWithGoogleNativeAsync()
    {
#if ANDROID
        try
        {
            await EnsureInitializedAsync();
            string webClientId = "851890940381-7g35rn7qv9m7b2r8sshnaugjhkt7vtc3.apps.googleusercontent.com";
            GoogleSignInTcs = new TaskCompletionSource<(string? Token, string Error)>();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    var context = Platform.CurrentActivity;
                    if (context == null)
                    {
                        GoogleSignInTcs.TrySetResult((null, AppResources.Auth_AndroidContextError));
                        return;
                    }

                    var gso = new Android.Gms.Auth.Api.SignIn.GoogleSignInOptions.Builder(Android.Gms.Auth.Api.SignIn.GoogleSignInOptions.DefaultSignIn)
                        .RequestIdToken(webClientId)
                        .RequestEmail()
                        .Build();

                    var googleSignInClient = Android.Gms.Auth.Api.SignIn.GoogleSignIn.GetClient(context, gso);
                    googleSignInClient.SignOut();

                    context.StartActivityForResult(googleSignInClient.SignInIntent, 9001);
                }
                catch (Exception threadEx)
                {
                    GoogleSignInTcs.TrySetResult((null, threadEx.Message));
                }
            });

            var result = await GoogleSignInTcs.Task;

            if (!string.IsNullOrEmpty(result.Token))
            {
                var session = await _supabaseClient!.Auth.SignInWithIdToken(Supabase.Gotrue.Constants.Provider.Google, result.Token);

                if (session != null)
                {
                    _currentSession = session;
                    await SecureStorage.Default.SetAsync("supabase_session", JsonConvert.SerializeObject(session));
                    return (true, AppResources.Auth_GoogleSignedIn, session);
                }
                return (false, AppResources.Auth_SupabaseSessionError, null);
            }
            else
            {
                if (result.Error != null && (result.Error.Contains("16") || result.Error.Contains("CANCELED")))
                    return (false, AppResources.Auth_CancelledByUser, null);

                return (false, AppResources.F(AppResources.Auth_GoogleFailed, result.Error), null);
            }
        }
        catch (Exception ex)
        {
            return (false, AppResources.F(AppResources.Auth_InternalError, ex.Message), null);
        }
#else
        return (false, AppResources.Auth_UseUniversalLogin, null);
#endif
    }

    // NUEVO MÉTODO PARA IPHONE (UNIVERSAL WEB AUTH)
    public async Task<(bool Success, string Message, Session? Session)> LoginWithGoogleUniversalAsync()
    {
        try
        {
            await EnsureInitializedAsync();

            var options = new SignInOptions { RedirectTo = "goldeenride://" };
            var state = await _supabaseClient!.Auth.SignIn(Supabase.Gotrue.Constants.Provider.Google, options);

            if (state?.Uri == null) return (false, AppResources.Auth_LinkError, null);

            var authResult = await WebAuthenticator.Default.AuthenticateAsync(
                new Uri(state.Uri.ToString()),
                new Uri("goldeenride://"));

            if (authResult?.Properties != null &&
                authResult.Properties.TryGetValue("access_token", out string? accessToken) &&
                authResult.Properties.TryGetValue("refresh_token", out string? refreshToken))
            {
                var session = await _supabaseClient.Auth.SetSession(accessToken, refreshToken);
                _currentSession = session;
                await SecureStorage.Default.SetAsync("supabase_session", JsonConvert.SerializeObject(session));
                return (true, AppResources.Auth_GoogleSignedIn, session);
            }

            return (false, AppResources.Auth_NoValidCredentials, null);
        }
        catch (TaskCanceledException)
        {
            return (false, AppResources.Auth_SignInCancelled, null);
        }
        catch (Exception ex)
        {
            return (false, AppResources.F(AppResources.Auth_InternalError, ex.Message), null);
        }
    }

    // ... (El resto de tus métodos de base de datos se quedan EXACTAMENTE igual a partir de aquí)
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

    // ─────────── CAMBIAR DE ROL ───────────
    // También pasa por la función "swift-worker": el servidor saca el id de la sesión,
    // cambia el rol con la función SQL cambiar_rol_usuario y borra las fotos de los
    // micros si se cerró una flota. Error: "mismo_rol", "codigo_invalido", "red"...
    public async Task<(bool Ok, string Error, string? IdJefe, string? NombreJefe, int PasajerosAvisados)> CambiarRolAsync(string nuevoRol, string? codigoFlota)
    {
        try
        {
            await EnsureInitializedAsync();
            if (_supabaseClient == null) return (false, "red", null, null, 0);

            try { await _supabaseClient.Auth.RefreshSession(); } catch { }
            string? token = _supabaseClient.Auth.CurrentSession?.AccessToken ?? _currentSession?.AccessToken;
            if (string.IsNullOrEmpty(token)) return (false, "sesion", null, null, 0);

            var payload = new Newtonsoft.Json.Linq.JObject
            {
                ["accion"] = "cambiar_rol",
                ["rol"] = nuevoRol,
                ["codigo"] = string.IsNullOrWhiteSpace(codigoFlota) ? null : codigoFlota.Trim().ToUpperInvariant()
            };

            using var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, $"{SupabaseUrl}/functions/v1/{FuncionEliminarCuenta}");
            request.Headers.Add("apikey", SupabaseAnonKey);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            request.Content = new System.Net.Http.StringContent(payload.ToString(), System.Text.Encoding.UTF8, "application/json");

            using var response = await _httpEliminar.SendAsync(request);
            string body = await response.Content.ReadAsStringAsync();
            var json = Newtonsoft.Json.Linq.JObject.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);

            if (json["ok"]?.ToObject<bool>() == true)
                return (true, "", json.Value<string>("id_jefe"), json.Value<string>("nombre_jefe"), json.Value<int?>("pasajeros_avisados") ?? 0);

            Debug.WriteLine($"❌ cambiar_rol ({(int)response.StatusCode}): {body}");
            return (false, json.Value<string>("error") ?? $"HTTP {(int)response.StatusCode}", null, null, 0);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ cambiar_rol: {ex.Message}");
            return (false, "red", null, null, 0);
        }
    }

    // ─────────── PERMISO DE SUB-JEFE ───────────
    // Solo funciona si quien llama es el jefe de ese empleado (lo revisa Supabase).
    public async Task<bool> FijarPermisoViajesAsync(string idEmpleado, bool permitir)
    {
        try
        {
            await EnsureInitializedAsync();
            if (_supabaseClient == null) return false;
            var resp = await _supabaseClient.Rpc("fijar_permiso_viajes", new Dictionary<string, object>
            {
                { "p_empleado", idEmpleado },
                { "p_permitir", permitir }
            });
            var json = Newtonsoft.Json.Linq.JObject.Parse(resp.Content ?? "{}");
            return json["ok"]?.ToObject<bool>() == true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ fijar_permiso_viajes: {ex.Message}");
            return false;
        }
    }

    // ─────────── ELIMINAR CUENTA ───────────
    // Llama a la Edge Function "eliminar-cuenta". El servidor saca el id del usuario
    // de SU sesión (no de lo que mande la app), así nadie puede borrar otra cuenta.
    // Supabase le puso el nombre automático "swift-worker" a la función de eliminar cuenta
    // (la misma función también hace el cambio de rol).
    private const string FuncionEliminarCuenta = "swift-worker";
    private static readonly System.Net.Http.HttpClient _httpEliminar = new() { Timeout = TimeSpan.FromSeconds(60) };

    public async Task<(bool Ok, string Error)> EliminarCuentaAsync()
    {
        try
        {
            await EnsureInitializedAsync();
            if (_supabaseClient == null) return (false, AppResources.Error_Connection);

            // Sesión fresca: si el token venció, el servidor respondería 401
            try { await _supabaseClient.Auth.RefreshSession(); } catch { }
            string? token = _supabaseClient.Auth.CurrentSession?.AccessToken ?? _currentSession?.AccessToken;
            if (string.IsNullOrEmpty(token))
            {
                await IsSessionActiveAsync();
                token = _supabaseClient.Auth.CurrentSession?.AccessToken ?? _currentSession?.AccessToken;
            }
            if (string.IsNullOrEmpty(token)) return (false, "Sin sesión / No session");

            using var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, $"{SupabaseUrl}/functions/v1/{FuncionEliminarCuenta}");
            request.Headers.Add("apikey", SupabaseAnonKey);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            request.Content = new System.Net.Http.StringContent("{\"confirmar\":\"ELIMINAR\"}", System.Text.Encoding.UTF8, "application/json");

            using var response = await _httpEliminar.SendAsync(request);
            string body = await response.Content.ReadAsStringAsync();
            bool ok = false;
            try { ok = Newtonsoft.Json.Linq.JObject.Parse(body)["ok"]?.ToObject<bool>() == true; } catch { }

            if (!response.IsSuccessStatusCode || !ok)
            {
                Debug.WriteLine($"❌ eliminar-cuenta ({(int)response.StatusCode}): {body}");
                return (false, $"HTTP {(int)response.StatusCode}");
            }

            // La cuenta ya no existe: se limpia la sesión guardada en el celular
            try { await _supabaseClient.Auth.SignOut(); } catch { }
            _currentSession = null;
            SecureStorage.Default.Remove("supabase_session");
            return (true, "");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ eliminar-cuenta: {ex.Message}");
            return (false, ex.Message);
        }
    }

    public GotrueUser? GetCurrentUser() => _supabaseClient?.Auth.CurrentUser;

    public async Task<Usuario?> GetUserDataAsync(string userId)
    {
        try
        {
            await EnsureInitializedAsync();
            if (_supabaseClient == null || string.IsNullOrEmpty(userId)) return null;
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

    public async Task<List<Usuario>> GetPublicFleetsAsync()
    {
        try
        {
            await EnsureInitializedAsync();
            if (_supabaseClient == null) return new List<Usuario>();

            var response = await _supabaseClient.From<Usuario>().Get();
            var allUsers = response.Models ?? new List<Usuario>();

            var flotas = allUsers.Where(u =>
                u.Rol.Contains("jefe", StringComparison.OrdinalIgnoreCase) ||
                u.Rol.Contains("owner", StringComparison.OrdinalIgnoreCase) ||
                u.Rol.Contains("independiente", StringComparison.OrdinalIgnoreCase)
            ).ToList();

            return flotas;
        }
        catch { return new List<Usuario>(); }
    }

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

    private static string GenerarCodigoAleatorio(int longitud)
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
            if (_supabaseClient == null) return (false, AppResources.Error_Connection);

            string codigoNormalizado = codigoFlota.Trim().ToUpperInvariant();
            var bossResponse = await _supabaseClient.From<Usuario>().Filter("codigo_flota", Supabase.Postgrest.Constants.Operator.Equals, codigoNormalizado).Get();
            var boss = bossResponse.Models?.FirstOrDefault();

            if (boss == null || !boss.Rol.ToLower().Contains("jefe"))
                return (false, AppResources.Fleet_InvalidCode);

            try
            {
                await _supabaseClient.From<Usuario>()
                    .Where(u => u.Id == employeeId)
                    .Set(u => u.IdJefe, boss.Id)
                    .Update();
            }
            catch (Exception updateEx) when (updateEx.Message.Contains("Sequence contains no elements")) { }

            return (true, AppResources.F(AppResources.Success_JoinedFleet, boss.Nombre));
        }
        catch (Exception ex)
        {
            return (false, AppResources.F(AppResources.Error_JoinFailed, ex.Message));
        }
    }

    public async Task<(bool Success, string Message)> LinkEmployeeByEmailAsync(string emailEmpleado, string idJefe)
    {
        try
        {
            await EnsureInitializedAsync();
            if (_supabaseClient == null) return (false, AppResources.Error_Connection);
            var response = await _supabaseClient.From<Usuario>().Where(u => u.Email == emailEmpleado.Trim()).Get();
            var empleado = response.Models?.FirstOrDefault();
            if (empleado == null) return (false, AppResources.Error_EmailNotFound);
            if (!empleado.Rol.Contains("empleado")) return (false, AppResources.Error_NotEmployee);
            if (!string.IsNullOrEmpty(empleado.IdJefe)) return (false, AppResources.Error_AlreadyHasBoss);
            empleado.IdJefe = idJefe;
            await empleado.Update<Usuario>();
            return (true, AppResources.Success_Linked);
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

    public async Task<bool> UpdateVehicleAsync(Vehiculo vehiculo)
    {
        try
        {
            await EnsureInitializedAsync();
            if (_supabaseClient == null) return false;
            await vehiculo.Update<Vehiculo>();
            return true;
        }
        catch { return false; }
    }

    public async Task<bool> DeleteVehicleAsync(string idVehiculo)
    {
        try
        {
            await EnsureInitializedAsync();
            if (_supabaseClient == null) return false;
            await _supabaseClient.From<Vehiculo>().Where(v => v.Id == idVehiculo).Delete();
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
            if (_supabaseClient == null || asignaciones.Count == 0) return (false, AppResources.Global_NoDataToSave);

            int batchSize = 50;
            for (int i = 0; i < asignaciones.Count; i += batchSize)
            {
                var lote = asignaciones.Skip(i).Take(batchSize).ToList();
                await _supabaseClient.From<Asignacion>().Insert(lote);
            }

            return (true, "");
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public async Task<(bool Success, string Error)> CreateParadasBulkAsync(List<ViajeParada> paradas)
    {
        try
        {
            await EnsureInitializedAsync();
            if (_supabaseClient == null || paradas.Count == 0) return (false, AppResources.Global_NoStopsToSave);
            await _supabaseClient.From<ViajeParada>().Insert(paradas);
            return (true, "");
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public async Task<List<ViajeParada>> GetParadasByViajeAsync(string idViaje)
    {
        try
        {
            await EnsureInitializedAsync();
            if (_supabaseClient == null) return new List<ViajeParada>();
            var resultado = await _supabaseClient.From<ViajeParada>()
                .Filter("id_viaje", Supabase.Postgrest.Constants.Operator.Equals, idViaje)
                .Order("orden", Supabase.Postgrest.Constants.Ordering.Ascending)
                .Get();
            return resultado.Models ?? new List<ViajeParada>();
        }
        catch { return new List<ViajeParada>(); }
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
                if (v.HoraSalida.Kind != DateTimeKind.Utc)
                    v.HoraSalida = DateTime.SpecifyKind(v.HoraSalida, DateTimeKind.Utc);
            }
            return modelos;
        }
        catch { return new List<Viaje>(); }
    }

    // 🆕 Trae UN solo viaje por su Id, en vez de traer la tabla `viajes` completa
    // (de todas las flotas) solo para buscar uno con FirstOrDefault en el celular.
    // Mismo patrón que GetAsignacionByIdAsync.
    public async Task<Viaje?> GetTripByIdAsync(string idViaje)
    {
        try
        {
            await EnsureInitializedAsync();
            if (_supabaseClient == null) return null;

            var resp = await _supabaseClient.From<Viaje>().Filter("id", Supabase.Postgrest.Constants.Operator.Equals, idViaje).Get();
            var viaje = resp.Models?.FirstOrDefault();

            if (viaje != null && viaje.HoraSalida.Kind != DateTimeKind.Utc)
                viaje.HoraSalida = DateTime.SpecifyKind(viaje.HoraSalida, DateTimeKind.Utc);

            return viaje;
        }
        catch { return null; }
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
                if (asig.Fecha.Kind != DateTimeKind.Utc)
                    asig.Fecha = DateTime.SpecifyKind(asig.Fecha, DateTimeKind.Utc);
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
            var asig = resp.Models?.FirstOrDefault();

            if (asig != null && asig.Fecha.Kind != DateTimeKind.Utc)
                asig.Fecha = DateTime.SpecifyKind(asig.Fecha, DateTimeKind.Utc);

            return asig;
        }
        catch { return null; }
    }

    public async Task<List<Asignacion>> GetAsignacionesByIdsAsync(List<string> ids)
    {
        try
        {
            await EnsureInitializedAsync();
            if (_supabaseClient == null || ids.Count == 0) return new List<Asignacion>();
            var resp = await _supabaseClient.From<Asignacion>().Filter("id", Supabase.Postgrest.Constants.Operator.In, ids).Get();
            var modelos = resp.Models ?? new List<Asignacion>();
            foreach (var asig in modelos)
            {
                if (asig.Fecha.Kind != DateTimeKind.Utc)
                    asig.Fecha = DateTime.SpecifyKind(asig.Fecha, DateTimeKind.Utc);
            }
            return modelos;
        }
        catch { return new List<Asignacion>(); }
    }

    public async Task<List<Viaje>> GetViajesByIdsAsync(List<string> ids)
    {
        try
        {
            await EnsureInitializedAsync();
            if (_supabaseClient == null || ids.Count == 0) return new List<Viaje>();
            var resp = await _supabaseClient.From<Viaje>().Filter("id", Supabase.Postgrest.Constants.Operator.In, ids).Get();
            var modelos = resp.Models ?? new List<Viaje>();
            foreach (var v in modelos)
            {
                if (v.HoraSalida.Kind != DateTimeKind.Utc)
                    v.HoraSalida = DateTime.SpecifyKind(v.HoraSalida, DateTimeKind.Utc);
            }
            return modelos;
        }
        catch { return new List<Viaje>(); }
    }

    public async Task<List<Vehiculo>> GetVehiculosByIdsAsync(List<string> ids)
    {
        try
        {
            await EnsureInitializedAsync();
            if (_supabaseClient == null || ids.Count == 0) return new List<Vehiculo>();
            var resp = await _supabaseClient.From<Vehiculo>().Filter("id", Supabase.Postgrest.Constants.Operator.In, ids).Get();
            var modelos = resp.Models ?? new List<Vehiculo>();
            return modelos;
        }
        catch { return new List<Vehiculo>(); }
    }

    public async Task<List<Usuario>> GetUsuariosByIdsAsync(List<string> ids)
    {
        try
        {
            await EnsureInitializedAsync();
            if (_supabaseClient == null || ids.Count == 0) return new List<Usuario>();
            var resp = await _supabaseClient.From<Usuario>().Filter("id", Supabase.Postgrest.Constants.Operator.In, ids).Get();
            return resp.Models ?? new List<Usuario>();
        }
        catch { return new List<Usuario>(); }
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

    public async Task<bool> UpdateRutaCompletaAsync(string idViaje, DateTime fechaDesde, string nuevoIdChofer, string nuevoIdVehiculo, TimeSpan nuevaHora, TimeSpan horaInicioRecorrido, TimeSpan horaLlegadaDestino, string metaTexto, double metaLat, double metaLon)
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
                    DateTime localDate = asig.Fecha.AHoraElSalvador().Date;
                    asig.Fecha = localDate.Add(nuevaHora).ToUniversalTime();
                }
                await _supabaseClient.From<Asignacion>().Upsert(response.Models);
            }

            var viajeResp = await _supabaseClient.From<Viaje>().Filter("id", Supabase.Postgrest.Constants.Operator.Equals, idViaje).Get();
            var viajeActual = viajeResp.Models?.FirstOrDefault();
            if (viajeActual != null)
            {
                DateTime templateLocalDate = viajeActual.HoraSalida.AHoraElSalvador().Date;
                viajeActual.HoraSalida = templateLocalDate.Add(nuevaHora).ToUniversalTime();
                viajeActual.HoraInicioRecorrido = horaInicioRecorrido;
                viajeActual.HoraLlegadaDestino = horaLlegadaDestino;
                viajeActual.MetaTexto = metaTexto;
                viajeActual.MetaLatitud = metaLat;
                viajeActual.MetaLongitud = metaLon;
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

    public async Task<bool> UpdateUserDataAsync(Usuario userData)
    {
        try
        {
            await EnsureInitializedAsync();
            await _supabaseClient!.From<Usuario>().Update(userData);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<UbicacionPasajero>> GetUbicacionesPasajeroAsync(string idPasajero)
    {
        try
        {
            await EnsureInitializedAsync();
            var response = await _supabaseClient!.From<UbicacionPasajero>()
                                                .Where(x => x.IdPasajero == idPasajero)
                                                .Get();

            return response.Models ?? new List<UbicacionPasajero>();
        }
        catch
        {
            return new List<UbicacionPasajero>();
        }
    }

    public async Task<bool> CreateUbicacionAsync(UbicacionPasajero ubicacion)
    {
        try
        {
            await EnsureInitializedAsync();
            await _supabaseClient!.From<UbicacionPasajero>().Insert(ubicacion);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteReservaAsync(string idReserva)
    {
        try
        {
            await EnsureInitializedAsync();
            if (_supabaseClient == null) return false;
            await _supabaseClient.From<Reserva>().Where(r => r.Id == idReserva).Delete();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error borrando reserva: {ex.Message}");
            return false;
        }
    }

    public async Task<Viaje?> GetViajeByIdAsync(string idViaje)
    {
        try
        {
            await EnsureInitializedAsync();
            if (_supabaseClient == null) return null;
            var response = await _supabaseClient.From<Viaje>().Where(v => v.Id == idViaje).Get();
            var viaje = response.Models?.FirstOrDefault();

            if (viaje != null)
            {
                if (viaje.HoraSalida.Kind == DateTimeKind.Unspecified)
                    viaje.HoraSalida = DateTime.SpecifyKind(viaje.HoraSalida, DateTimeKind.Utc);
                else if (viaje.HoraSalida.Kind == DateTimeKind.Local)
                    viaje.HoraSalida = viaje.HoraSalida.ToUniversalTime();
            }

            return viaje;
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<Reserva>> GetReservasPorPasajeroYRangoAsync(string idPasajero, DateTime inicio, DateTime fin)
    {
        try
        {
            await EnsureInitializedAsync();
            if (_supabaseClient == null) return new List<Reserva>();

            var response = await _supabaseClient.From<Reserva>()
                .Where(r => r.IdPasajero == idPasajero)
                .Get();

            return response.Models ?? new List<Reserva>();
        }
        catch
        {
            return new List<Reserva>();
        }
    }

    public async Task UpsertUbicacionEnVivoAsync(PassGold.Models.UbicacionEnVivo ubicacion)
    {
        try
        {
            await EnsureInitializedAsync();
            await _supabaseClient!.From<PassGold.Models.UbicacionEnVivo>().Upsert(ubicacion);
        }
        catch { }
    }

    public async Task<List<UbicacionEnVivo>> GetUbicacionesEnVivoPorAsignacionAsync(string idAsignacion)
    {
        try
        {
            var response = await _supabaseClient.From<UbicacionEnVivo>()
                                        .Where(x => x.IdAsignacion == idAsignacion)
                                        .Get();
            return response.Models;
        }
        catch
        {
            return new List<UbicacionEnVivo>();
        }
    }
}