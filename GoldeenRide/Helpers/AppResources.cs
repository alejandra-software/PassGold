using System.Globalization;

namespace GoldeenRide.Helpers;

public static class AppResources
{
    // Blindamos la lectura del idioma del sistema
    private static bool IsSpanish
    {
        get
        {
            try
            {
                return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "es";
            }
            catch
            {
                // Si Android bloquea la lectura, asumimos español por defecto en vez de crashear
                return true;
            }
        }
    }

    // --- MENU SHELL ---
    public static string MenuHome => IsSpanish ? "Inicio" : "Home";
    public static string MenuProfile => IsSpanish ? "Perfil" : "Profile";
    public static string MenuAdminFleet => IsSpanish ? "Administrar Flota" : "Admin Fleet";
    public static string MenuSchedule => IsSpanish ? "Programar Viajes" : "Schedule Trips";
    public static string MenuMyVehicle => IsSpanish ? "Mi Vehículo" : "My Vehicle";
    public static string MenuJoinFleet => IsSpanish ? "Unirse a Flota" : "Join Fleet";

    // --- DASHBOARD ---
    public static string Dashboard_OperationsToday => IsSpanish ? "Operación de Hoy" : "Operations Today";
    public static string Dashboard_MyWeek => IsSpanish ? "Mi Semana" : "My Week";
    public static string Dashboard_NoTripsToday => IsSpanish ? "No tienes viajes programados para hoy." : "No trips scheduled for today.";
    public static string Dashboard_MapRoute => IsSpanish ? "Ruta en Mapa" : "Route on Map";
    public static string Dashboard_MapDetails => IsSpanish ? "Detalles Mapbox" : "Mapbox Details";
    public static string Dashboard_PassengerList => IsSpanish ? "Listado de Abordaje" : "Boarding List";
    public static string Dashboard_WeeklySchedule => IsSpanish ? "Agenda Semanal" : "Weekly Schedule";
    public static string Dashboard_BackToToday => IsSpanish ? "Volver a Hoy" : "Back to Today";
    public static string Dashboard_EmptySchedule => IsSpanish ? "La agenda de la flota está vacía para esta semana." : "The fleet schedule is empty for this week.";
    public static string Dashboard_ViewDetail => IsSpanish ? "Ver Detalle" : "View Detail";
    public static string Dashboard_QuickActions => IsSpanish ? "Acciones Rápidas" : "Quick Actions";
    public static string Dashboard_Schedule => IsSpanish ? "Programar" : "Schedule";
    public static string Dashboard_MyRoutes => IsSpanish ? "Mis Rutas" : "My Routes";
    public static string Dashboard_OpsCenter => IsSpanish ? "Centro de Operaciones" : "Operations Center";
    public static string Dashboard_Loading => IsSpanish ? "Cargando..." : "Loading...";
    public static string Dashboard_From => IsSpanish ? "Del" : "From";
    public static string Dashboard_To => IsSpanish ? "al" : "to";
    public static string Dashboard_Arrival => IsSpanish ? "Llegada: " : "Arrival: ";
    public static string Dashboard_Departure => IsSpanish ? "Salida: " : "Departure: ";
    public static string Dashboard_Unknown => IsSpanish ? "Desconocido" : "Unknown";
    public static string Dashboard_ExtraBuses => IsSpanish ? "micros" : "buses";
    public static string Dashboard_FinalDestination => IsSpanish ? "Destino Final" : "Final Destination";
    public static string Dashboard_Pickup => IsSpanish ? "Recogida:" : "Pickup:";
    public static string Dashboard_Dropoff => IsSpanish ? "Bajada Alterna:" : "Drop-off:";

    // --- SCHEDULE TRIP ---
    public static string Schedule_Title => IsSpanish ? "Programar Flotilla" : "Schedule Fleet";
    public static string Schedule_Section1 => IsSpanish ? "1. Detalles del Recorrido" : "1. Route Details";
    public static string Schedule_TripType => IsSpanish ? "Tipo de Viaje" : "Trip Type";
    public static string Schedule_DepartureTime => IsSpanish ? "Hora de Salida" : "Departure Time";
    public static string Schedule_GeneralRoute => IsSpanish ? "Ruta General" : "General Route";
    public static string Schedule_RoutePlaceholder => IsSpanish ? "Describe la ruta..." : "Describe the route...";
    public static string Schedule_StartsOn => IsSpanish ? "Inicia el:" : "Starts on:";
    public static string Schedule_EndsOn => IsSpanish ? "Termina el:" : "Ends on:";
    public static string Schedule_Weekdays => IsSpanish ? "Días de la semana" : "Weekdays";
    public static string Schedule_Section2 => IsSpanish ? "2. Flotilla a Despachar" : "2. Dispatch Fleet";
    public static string Schedule_PublishTrips => IsSpanish ? "Publicar Viajes" : "Publish Trips";
    public static string Schedule_SelectDriverBus => IsSpanish ? "Por favor selecciona un Chofer y un Microbús." : "Please select a Driver and a Bus.";
    public static string Schedule_DriverAlreadyInList => IsSpanish ? "Ese chofer o microbús ya está en la lista." : "That driver or bus is already in the list.";
    public static string Schedule_AddAtLeastOne => IsSpanish ? "Añade al menos un Chofer y un Microbús." : "Add at least one Driver and Bus.";
    public static string Schedule_SelectOneDay => IsSpanish ? "Selecciona al menos un día." : "Select at least one day.";
    public static string Schedule_ScheduleClash => IsSpanish ? "Choque de Horario" : "Schedule Clash";
    public static string Schedule_ClashMessage => IsSpanish ? "Choque con {0} y vehículo {1}" : "Clash with {0} and vehicle {1}";
    public static string Schedule_Fix => IsSpanish ? "Corregir" : "Fix";
    public static string Schedule_DBError => IsSpanish ? "Error de BD" : "DB Error";
    public static string Schedule_RouteNotSaved => IsSpanish ? "No se guardó: {0}" : "Not saved: {0}";
    public static string Schedule_Success => IsSpanish ? "¡Éxito!" : "Success!";
    public static string Schedule_SuccessMessage => IsSpanish ? "Programados: {0} viajes" : "Scheduled: {0} trips";
    public static string Schedule_RealWindowHint => IsSpanish ? "Ventana real de ejecución (para saber cuándo está 'en curso' en el Dashboard):" : "Real execution window (used to know when it's 'in progress' on the Dashboard):";
    public static string Schedule_RealStartTime => IsSpanish ? "Inicio real (recogiendo gente)" : "Real start (picking up people)";
    public static string Schedule_RealArrivalTime => IsSpanish ? "Llegada real aprox." : "Approx. real arrival";

    // --- DASHBOARD: OPERACIÓN DE HOY ---
    public static string Dashboard_TripsInProgress => IsSpanish ? "🚀 Viajes en Ejecución" : "🚀 Trips in Progress";
    public static string Dashboard_NoTripInProgress => IsSpanish ? "No hay ningún viaje en ejecución en este momento." : "No trip is in progress right now.";
    public static string Dashboard_NextTrip => IsSpanish ? "Próximo viaje de hoy:" : "Next trip today:";

    // --- TRIP TYPES ---
    public static string TripType_Inbound => IsSpanish ? "Ida (Hacia Universidad)" : "Inbound (To University)";
    public static string TripType_Outbound => IsSpanish ? "Regreso (Hacia Casa)" : "Outbound (To Home)";
    public static string TripType_Special => IsSpanish ? "Especial" : "Special";

    // --- GLOBALS & ERRORS ---
    public static string Global_Ok => IsSpanish ? "OK" : "OK";
    public static string Global_Attention => IsSpanish ? "Atención" : "Attention";
    public static string Global_Error => IsSpanish ? "Error" : "Error";
    public static string Error_ConnectionFailed => IsSpanish ? "No se pudo conectar" : "Could not connect";
    public static string Success_Registration => IsSpanish ? "Registro exitoso." : "Registration successful.";
    public static string Error_Connection => IsSpanish ? "Error de conexión" : "Connection error";
    public static string Success_Login => IsSpanish ? "Sesión iniciada" : "Logged in";
    public static string Error_InvalidCredentials => IsSpanish ? "Correo o contraseña incorrectos." : "Incorrect email or password.";
    public static string Error_EmailNotFound => IsSpanish ? "No se encontró el correo." : "Email not found.";
    public static string Error_NotEmployee => IsSpanish ? "Este usuario no es Chofer Empleado." : "This user is not an Employee Driver.";
    public static string Error_AlreadyHasBoss => IsSpanish ? "Ya trabaja para otro jefe." : "Already works for another boss.";
    public static string Success_Linked => IsSpanish ? "¡Vinculado con éxito!" : "Successfully linked!";
    public static string Error_InvalidFleetCode => IsSpanish ? "El código ingresado no pertenece a un Chofer Jefe válido." : "The entered code does not belong to a valid Fleet Manager.";
    public static string Success_JoinedFleet => IsSpanish ? "¡Te has unido con éxito a la flota de {0}!" : "Successfully joined {0}'s fleet!";
    public static string Error_JoinFailed => IsSpanish ? "Error al unirse: Asegúrate que el código no tenga espacios. Detalle: {0}" : "Error joining: Make sure the code has no spaces. Detail: {0}";
    public static string Error_NoSupabaseConnection => IsSpanish ? "No hay conexión a Supabase." : "No connection to Supabase.";
    public static string Error_EmptyList => IsSpanish ? "No hay conexión o la lista está vacía." : "No connection or the list is empty.";

    // --- AUTH & LOGIN ---
    public static string AppTitle => "GoldeenRide";
    public static string AppSubtitle => IsSpanish ? "Tu transporte, a tiempo" : "Your ride, on time";
    public static string LoginTitle => IsSpanish ? "Iniciar Sesión" : "Login";
    public static string LoginEmail => IsSpanish ? "Correo Electrónico" : "Email";
    public static string LoginPassword => IsSpanish ? "Contraseña" : "Password";
    public static string LoginButton => IsSpanish ? "Entrar" : "Sign In";
    public static string LoginNoAccount => IsSpanish ? "¿No tienes cuenta?" : "Don't have an account?";

    // --- REGISTER STEP 1 ---
    public static string RegisterStep1Title => IsSpanish ? "Regístrate" : "Sign Up";
    public static string RegisterStep1Subtitle => IsSpanish ? "Paso 1 de 2: Credenciales" : "Step 1 of 2: Credentials";
    public static string RegisterStep1Email => IsSpanish ? "Correo" : "Email";
    public static string RegisterStep1EmailHint => IsSpanish ? "tu@correo.com" : "you@email.com";
    public static string RegisterStep1Password => IsSpanish ? "Crear Contraseña" : "Create Password";
    public static string RegisterStep1PasswordHint => IsSpanish ? "Mínimo 6 caracteres" : "Min 6 characters";
    public static string RegisterStep1ConfirmPassword => IsSpanish ? "Confirmar Contraseña" : "Confirm Password";
    public static string RegisterStep1NextButton => IsSpanish ? "Siguiente" : "Next";
    public static string RegisterStep1HaveAccount => IsSpanish ? "¿Ya tienes cuenta?" : "Already have an account?";

    // --- REGISTER STEP 2 ---
    public static string RegisterStep2Title => IsSpanish ? "Perfil" : "Profile";
    public static string RegisterStep2Subtitle => IsSpanish ? "Paso 2 de 2: Datos Personales" : "Step 2 of 2: Personal Data";
    public static string RegisterStep2Name => IsSpanish ? "Nombre Completo" : "Full Name";
    public static string RegisterStep2NameHint => IsSpanish ? "Ej. Juan Pérez" : "e.g. John Doe";
    public static string RegisterStep2UserType => IsSpanish ? "Tipo de Usuario" : "User Type";
    public static string RegisterStep2RolePassenger => IsSpanish ? "Pasajero" : "Passenger";
    public static string RegisterStep2RoleDriverOwner => IsSpanish ? "Chofer Jefe (Dueño)" : "Driver Owner";
    public static string RegisterStep2RoleDriverEmployee => IsSpanish ? "Chofer Empleado" : "Driver Employee";
    public static string RegisterStep2RoleDriverIndependent => IsSpanish ? "Chofer Independiente" : "Independent Driver";
    public static string RegisterStep2Photo => IsSpanish ? "Foto de Perfil" : "Profile Photo";
    public static string RegisterStep2PhotoButton => IsSpanish ? "Seleccionar Foto" : "Select Photo";
    public static string RegisterStep2PhotoOptional => IsSpanish ? "(Opcional)" : "(Optional)";
    public static string RegisterStep2RegisterButton => IsSpanish ? "Completar Registro" : "Complete Registration";
    public static string RegisterStep2BackButton => IsSpanish ? "Atrás" : "Back";

    // --- ACTIVE TRIP ---
    public static string ActiveTrip_MapPlaceholder => IsSpanish ? "Espacio Reservado para Mapbox" : "Mapbox Reserved Space";
    public static string ActiveTrip_Pickup => IsSpanish ? "Recogida" : "Pickup";
    public static string ActiveTrip_AltDropoff => IsSpanish ? "Bajada Alterna" : "Alt Drop-off";
    public static string ActiveTrip_Destination => IsSpanish ? "Destino (U)" : "Destination (U)";
    public static string ActiveTrip_ViewAll => IsSpanish ? "Ver Todos" : "View All";
    public static string ActiveTrip_FinishBtn => IsSpanish ? "Finalizar Recorrido" : "Finish Trip";

    // --- ADD VEHICLE ---
    public static string AddVehicleTitle => IsSpanish ? "Añadir Microbús" : "Add Minibus";
    public static string AddVehicleHeader => IsSpanish ? "Registrar Nuevo Vehículo" : "Register New Vehicle";
    public static string AddVehicleDesc => IsSpanish ? "Añade un nuevo microbús a tu flota." : "Add a new minibus to your fleet.";
    public static string AddVehiclePlateLabel => IsSpanish ? "Placa del Microbús" : "Minibus Plate";
    public static string AddVehiclePlatePlaceholder => IsSpanish ? "Ej. MB-1234" : "e.g. MB-1234";
    public static string AddVehicleCapacityLabel => IsSpanish ? "Capacidad (Pasajeros)" : "Capacity (Passengers)";
    public static string AddVehicleCapacityPlaceholder => IsSpanish ? "Ej. 15" : "e.g. 15";
    public static string AddVehicleSaveBtn => IsSpanish ? "Guardar Microbús" : "Save Minibus";

    // --- ADMIN FLEET ---
    public static string AdminFleet_Title => IsSpanish ? "Mi Flota y Empleados" : "My Fleet and Employees";
    public static string AdminFleet_MyBuses => IsSpanish ? "Mis Microbuses" : "My Minibuses";
    public static string AdminFleet_AddBtn => IsSpanish ? "+ Añadir" : "+ Add";
    public static string AdminFleet_MyDrivers => IsSpanish ? "Mis Choferes" : "My Drivers";
    public static string AdminFleet_NoEmployees => IsSpanish ? "Aún no tienes empleados en tu flota." : "You don't have employees in your fleet yet.";
    public static string AdminFleet_Instructions => IsSpanish ? "Ve a 'Mi Perfil', copia tu Código de Flota y envíaselo por WhatsApp a tus choferes." : "Go to 'My Profile', copy your Fleet Code and send it via WhatsApp to your drivers.";

    // --- EDIT TRIP ---
    public static string EditTrip_Title => IsSpanish ? "Reasignar Viaje" : "Reassign Trip";
    public static string EditTrip_WarningTitle => IsSpanish ? "⚠️ Modo Edición" : "⚠️ Edit Mode";
    public static string EditTrip_WarningDesc => IsSpanish ? "Usa esta pantalla para hacer cambios de emergencia si un chofer se enferma o un microbús se daña." : "Use this screen to make emergency changes if a driver gets sick or a bus breaks down.";
    public static string EditTrip_TripData => IsSpanish ? "Datos del Viaje" : "Trip Data";
    public static string EditTrip_Route => IsSpanish ? "Ruta:" : "Route:";
    public static string EditTrip_Departure => IsSpanish ? "Hora Salida:" : "Departure:";
    public static string EditTrip_NewAssign => IsSpanish ? "Nueva Asignación" : "New Assignment";
    public static string EditTrip_SelectDriver => IsSpanish ? "Seleccionar Chofer (Activos)" : "Select Driver (Active)";
    public static string EditTrip_SelectBus => IsSpanish ? "Seleccionar Microbús (Activos)" : "Select Minibus (Active)";
    public static string EditTrip_SaveBtn => IsSpanish ? "Guardar Cambios" : "Save Changes";

    // --- PASSENGER DASHBOARD ---
    public static string PassengerDash_Hello => IsSpanish ? "Hola," : "Hello,";
    public static string PassengerDash_WhereTo => IsSpanish ? "¿A dónde viajamos hoy?" : "Where are we traveling today?";
    public static string PassengerDash_ViewTripsDate => IsSpanish ? "Ver viajes para la fecha:" : "View trips for date:";
    public static string PassengerDash_AvailableTrips => IsSpanish ? "Viajes Disponibles" : "Available Trips";
    public static string PassengerDash_NoTrips => IsSpanish ? "No hay viajes programados para esta fecha." : "There are no trips scheduled for this date.";
    public static string PassengerDash_BookSeat => IsSpanish ? "Reservar Asiento" : "Book Seat";

    // --- SETTINGS PAGE ---
    public static string SettingsPageTitle => IsSpanish ? "Ajustes y Perfil" : "Settings & Profile";
    public static string SettingsInviteTitle => IsSpanish ? "Invitar a mi Flota" : "Invite to my Fleet";
    public static string SettingsInviteDesc => IsSpanish ? "Comparte este código con tus choferes empleados para que se unan a tu flota:" : "Share this code with your employee drivers so they can join your fleet:";
    public static string SettingsCopyBtn => IsSpanish ? "Copiar Código" : "Copy Code";
    public static string SettingsMyFleetTitle => IsSpanish ? "Mi Flota" : "My Fleet";
    public static string SettingsPasteDesc => IsSpanish ? "Pega aquí el código que te envió tu jefe para unirte a su flota:" : "Paste the code your boss sent you here to join their fleet:";
    public static string SettingsInputPlaceholder => IsSpanish ? "Código de Flota" : "Fleet Code";
    public static string SettingsJoinBtn => IsSpanish ? "Vincular Cuenta" : "Link Account";
    public static string SettingsWorkingFor => IsSpanish ? "Trabajando para:" : "Working for:";
    public static string SettingsLogoutBtn => IsSpanish ? "Cerrar Sesión" : "Log Out";

    // --- TRIP DETAILS PAGE ---
    public static string TripDetails_Title => IsSpanish ? "Detalle del Viaje" : "Trip Details";
    public static string TripDetails_LogisticsTitle => IsSpanish ? "Logística de Ruta" : "Route Logistics";
    public static string TripDetails_InspectPrompt => IsSpanish ? "Selecciona qué unidad deseas inspeccionar en este horario:" : "Select which unit you want to inspect at this time:";
    public static string TripDetails_SelectDriver => IsSpanish ? "Seleccionar chofer/unidad..." : "Select driver/unit...";
    public static string TripDetails_TabPassengers => IsSpanish ? "👥 Pasajeros" : "👥 Passengers";
    public static string TripDetails_TabMap => IsSpanish ? "🗺️ Mapa" : "🗺️ Map";
    public static string TripDetails_TabOperator => IsSpanish ? "🚐 Operador" : "🚐 Operator";
    public static string TripDetails_BoardingList => IsSpanish ? "Lista de Abordaje" : "Boarding List";
    public static string TripDetails_NoPassengers => IsSpanish ? "No hay pasajeros registrados en esta unidad." : "There are no passengers registered in this unit.";
    public static string TripDetails_RealTimeRoute => IsSpanish ? "Ruta en Tiempo Real" : "Real Time Route";
    public static string TripDetails_MapPlaceholder => IsSpanish ? "🗺️ Lienzo reservado para el Mapa interactivo" : "🗺️ Canvas reserved for the interactive Map";
    public static string TripDetails_UnitData => IsSpanish ? "Datos de la Unidad" : "Unit Data";
    public static string TripDetails_AssignedDriver => IsSpanish ? "Chofer Asignado" : "Assigned Driver";
    public static string TripDetails_TransportUnit => IsSpanish ? "Unidad de Transporte" : "Transport Unit";
}