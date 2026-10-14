using System.Globalization;

namespace PassGold.Helpers;

public static class AppResources
{
    // Español solo si el idioma de la app (automático = el del celular) es español.
    // Cualquier otro idioma que la app no tenga → inglés.
    public static bool IsSpanish
    {
        get
        {
            try { return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "es"; }
            catch { return false; }
        }
    }

    private static readonly CultureInfo CulturaEs = CultureInfo.GetCultureInfo("es-SV");
    private static readonly CultureInfo CulturaEn = CultureInfo.GetCultureInfo("en-US");

    /// <summary>
    /// Cultura para MOSTRAR fechas y horas en el mismo idioma que los textos
    /// ("lun 5 oct · 6:50 a. m." en español, "Mon Oct 5 · 6:50 AM" en inglés).
    /// Úsala en ToString("...", AppResources.Culture) en vez de new CultureInfo("es-SV").
    /// </summary>
    public static CultureInfo Culture => IsSpanish ? CulturaEs : CulturaEn;

    /// <summary>Formatea con la cultura de la app: AppResources.F(AppResources.X, a, b)</summary>
    public static string F(string formato, params object?[] args) => string.Format(Culture, formato, args);

    // ─────────── TIPOS DE VIAJE ───────────
    // En la base de datos el tipo SIEMPRE se guarda en español (así lo leen todas las
    // pantallas y las funciones del servidor). Solo se traduce para mostrarlo.
    public const string TripTypeDb_Inbound = "Ida (Hacia Universidad)";
    public const string TripTypeDb_Outbound = "Regreso (Hacia Casa)";
    public const string TripTypeDb_Special = "Especial";

    /// <summary>¿Es un viaje de ida (hacia la U)? Reconoce el valor en español y, por si acaso, el viejo en inglés.</summary>
    public static bool IsInboundTrip(string? tipo) =>
        !string.IsNullOrEmpty(tipo) &&
        (tipo.Contains("Ida", StringComparison.OrdinalIgnoreCase) || tipo.Contains("Inbound", StringComparison.OrdinalIgnoreCase));

    public static bool IsOutboundTrip(string? tipo) =>
        !string.IsNullOrEmpty(tipo) &&
        (tipo.Contains("Regreso", StringComparison.OrdinalIgnoreCase) || tipo.Contains("Outbound", StringComparison.OrdinalIgnoreCase));

    /// <summary>Valor de la BD → texto en el idioma de la app.</summary>
    public static string TripTypeLabel(string? tipoDb)
    {
        if (string.IsNullOrWhiteSpace(tipoDb)) return Trip_Generic;
        if (IsInboundTrip(tipoDb)) return TripType_Inbound;
        if (IsOutboundTrip(tipoDb)) return TripType_Outbound;
        if (tipoDb.Contains("Especial", StringComparison.OrdinalIgnoreCase) || tipoDb.Contains("Special", StringComparison.OrdinalIgnoreCase)) return TripType_Special;
        return tipoDb;
    }

    /// <summary>Texto del Picker (en cualquier idioma) → valor que se guarda en la BD.</summary>
    public static string TripTypeToDb(string? etiqueta)
    {
        if (IsInboundTrip(etiqueta)) return TripTypeDb_Inbound;
        if (IsOutboundTrip(etiqueta)) return TripTypeDb_Outbound;
        return TripTypeDb_Special;
    }

    /// <summary>Abreviaturas Lu..Do / Mo..Su, empezando en lunes.</summary>
    public static string[] WeekdayShortNames => new[] { Day_MonShort, Day_TueShort, Day_WedShort, Day_ThuShort, Day_FriShort, Day_SatShort, Day_SunShort };

    // --- MENU SHELL ---
    public static string MenuHome => IsSpanish ? "Inicio" : "Home";
    public static string MenuProfile => IsSpanish ? "Perfil" : "Profile";
    public static string MenuAdminFleet => IsSpanish ? "Administrar Flota" : "Admin Fleet";
    public static string MenuSchedule => IsSpanish ? "Programar Viajes" : "Schedule Trips";
    public static string MenuMyVehicle => IsSpanish ? "Mi Vehículo" : "My Vehicle";
    public static string MenuJoinFleet => IsSpanish ? "Unirse a Flota" : "Join Fleet";

    // --- DASHBOARD GENERAL ---
    public static string Dashboard_OperationsToday => IsSpanish ? "Operación de Hoy" : "Operations Today";
    public static string Dashboard_MyWeek => IsSpanish ? "Mi Semana" : "My Week";
    public static string Dashboard_NoTripsToday => IsSpanish ? "No tienes viajes programados para hoy." : "No trips scheduled for today.";
    public static string Dashboard_MapRoute => IsSpanish ? "Ruta en Mapa" : "Route on Map";
    public static string Dashboard_MapDetails => IsSpanish ? "Detalles Mapbox" : "Mapbox Details";
    public static string Dashboard_PassengerList => IsSpanish ? "Listado de Abordaje" : "Boarding List";
    public static string Dashboard_WeeklySchedule => IsSpanish ? "Agenda Semanal" : "Weekly Schedule";
    public static string Dashboard_BackToToday => IsSpanish ? "Volver a Hoy" : "Back to Today";
    public static string Dashboard_EmptySchedule => IsSpanish ? "La agenda está vacía para esta semana." : "The schedule is empty for this week.";
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
    public static string Dashboard_TripsInProgress => IsSpanish ? "Viajes en Curso / Próximos" : "Trips in Progress / Upcoming";
    public static string Dashboard_NextTrip => IsSpanish ? "Próximo Viaje" : "Next Trip";

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
    public static string Day_MonShort => IsSpanish ? "Lu" : "Mo";
    public static string Day_TueShort => IsSpanish ? "Ma" : "Tu";
    public static string Day_WedShort => IsSpanish ? "Mi" : "We";
    public static string Day_ThuShort => IsSpanish ? "Ju" : "Th";
    public static string Day_FriShort => IsSpanish ? "Vi" : "Fr";
    public static string Day_SatShort => IsSpanish ? "Sa" : "Sa";
    public static string Day_SunShort => IsSpanish ? "Do" : "Su";
    public static string Schedule_DriverPicker => IsSpanish ? "Chofer" : "Driver";
    public static string Schedule_BusPicker => IsSpanish ? "Microbús" : "Bus";
    public static string Schedule_ChooseFrequent => IsSpanish ? "📍 Elegir destino frecuente..." : "📍 Choose frequent destination...";
    public static string Schedule_SaveError => IsSpanish ? "No se pudo guardar el viaje. Intenta de nuevo." : "Couldn't save the trip. Try again.";
    public static string Schedule_TripSaved => IsSpanish ? "Viaje programado con éxito." : "Trip scheduled successfully.";
    public static string Schedule_AddAtLeastOne => IsSpanish ? "Añade al menos un Chofer y un Microbús." : "Add at least one Driver and Bus.";
    public static string Schedule_AddAssignment => IsSpanish ? "+ Agregar chofer y microbús" : "+ Add driver and bus";
    public static string Schedule_SelectOneDay => IsSpanish ? "Selecciona al menos un día." : "Select at least one day.";
    public static string Schedule_ScheduleClash => IsSpanish ? "Choque de Horario" : "Schedule Clash";
    public static string Schedule_ClashMessage => IsSpanish ? "Choque con {0} y vehículo {1}" : "Clash with {0} and vehicle {1}";
    public static string Schedule_Fix => IsSpanish ? "Corregir" : "Fix";
    public static string Schedule_DBError => IsSpanish ? "Error de BD" : "DB Error";
    public static string Schedule_RouteNotSaved => IsSpanish ? "No se guardó: {0}" : "Not saved: {0}";
    public static string Schedule_Success => IsSpanish ? "¡Éxito!" : "Success!";
    public static string Schedule_SuccessMessage => IsSpanish ? "Programados: {0} viajes" : "Scheduled: {0} trips";
    public static string Schedule_RealWindowHint => IsSpanish ? "Ventana real de ejecución:" : "Real execution window:";
    public static string Schedule_RealStartTime => IsSpanish ? "Inicio real (recogiendo gente)" : "Real start (picking up people)";
    public static string Schedule_RealArrivalTime => IsSpanish ? "Llegada real aprox." : "Approx. real arrival";

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

    // --- AGREGAR / EDITAR VEHÍCULO ---
    public static string AddVehicleTitle => IsSpanish ? "Añadir Microbús" : "Add Minibus";
    public static string AddVehicleHeader => IsSpanish ? "Nueva Unidad" : "New Unit";
    public static string AddVehicleDesc => IsSpanish ? "Registra los datos del microbús para tu flota." : "Register the minibus details for your fleet.";
    public static string AddVehiclePlateLabel => IsSpanish ? "Placa" : "License Plate";
    public static string AddVehiclePlatePlaceholder => IsSpanish ? "Ej. N1234" : "E.g. N1234";
    public static string AddVehicleCapacityLabel => IsSpanish ? "Capacidad de Pasajeros" : "Passenger Capacity";
    public static string AddVehicleCapacityPlaceholder => IsSpanish ? "Ej. 35" : "E.g. 35";
    public static string AddVehicleSaveBtn => IsSpanish ? "Guardar Microbús" : "Save Minibus";
    public static string AlertEmptyFields => IsSpanish ? "Por favor llena todos los campos." : "Please fill in all fields.";
    public static string AlertInvalidCapacity => IsSpanish ? "La capacidad debe ser un número válido." : "Capacity must be a valid number.";
    public static string AlertVehicleSaved => IsSpanish ? "El microbús ha sido guardado correctamente." : "The minibus has been saved successfully.";
    public static string AlertVehicleSaveError => IsSpanish ? "No se pudo guardar el vehículo." : "Could not save the vehicle.";
    public static string EditVehicle_Title => IsSpanish ? "Editar Microbús" : "Edit Minibus";
    public static string EditVehicle_Header => IsSpanish ? "Editar Unidad" : "Edit Unit";
    public static string EditVehicle_Desc => IsSpanish ? "Actualiza los datos de este microbús." : "Update this minibus's details.";
    public static string EditVehicle_PhotoLabel => IsSpanish ? "Foto de la Unidad" : "Unit Photo";
    public static string EditVehicle_SaveBtn => IsSpanish ? "Guardar Cambios" : "Save Changes";
    public static string AlertVehicleUpdated => IsSpanish ? "El microbús ha sido actualizado." : "The minibus has been updated.";
    public static string AlertVehicleUpdateError => IsSpanish ? "No se pudo actualizar el vehículo." : "Could not update the vehicle.";
    public static string AlertVehicleDeleted => IsSpanish ? "El microbús ha sido eliminado." : "The minibus has been deleted.";

    // --- AUTH & LOGIN ---
    public static string AppTitle => "PassGold";
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

    // --- PASSENGER DASHBOARD ---
    public static string PassengerDash_Hello => IsSpanish ? "Hola," : "Hello,";
    public static string PassengerDash_WhereTo => IsSpanish ? "¿A dónde viajamos hoy?" : "Where are we traveling today?";
    public static string PassengerDash_ViewTripsDate => IsSpanish ? "Mostrar viajes a partir de:" : "Show trips starting from:";
    public static string PassengerDash_AvailableTrips => IsSpanish ? "Viajes Disponibles" : "Available Trips";
    public static string PassengerDash_NoTrips => IsSpanish ? "No hay viajes programados en estas fechas." : "There are no trips scheduled for these dates.";
    public static string PassengerDash_BookSeat => IsSpanish ? "Reservar Asiento" : "Book Seat";
    public static string PassengerDash_DirectoryTitle => IsSpanish ? "Directorio de Flotas" : "Fleet Directory";
    public static string PassengerDash_DirectoryDesc => IsSpanish ? "Selecciona una unidad para ver su agenda." : "Select a unit to view its schedule.";
    public static string PassengerDash_ManagedBy => IsSpanish ? "Administrado por: {0}" : "Managed by: {0}";
    public static string PassengerDash_ContactDriver => IsSpanish ? "📞 Contactar chofer" : "📞 Contact Driver";
    public static string PassengerDash_CoveredRoutes => IsSpanish ? "Rutas Cubiertas:" : "Covered Routes:";
    public static string PassengerDash_ConfirmBooking => IsSpanish ? "Confirmar Reserva" : "Confirm Booking";
    public static string PassengerDash_ExactLocation => IsSpanish ? "Indicación exacta (Obligatorio)" : "Exact indication (Required)";
    public static string PassengerDash_MapPlaceholder => IsSpanish ? "Espacio Reservado para Mapbox\n(Toca para fijar el pin de ubicación)" : "Mapbox Reserved Space\n(Tap to pin location)";
    public static string PassengerDash_DriverUnit => IsSpanish ? "🚐 Unidad: " : "🚐 Unit: ";
    public static string PassengerDash_DriverName => IsSpanish ? "👤 Chofer: " : "👤 Driver: ";
    public static string PassengerDash_OfficialDeparture => IsSpanish ? "Salida Oficial: {0}" : "Official Departure: {0}";
    public static string PassengerDash_BookWholeMonth => IsSpanish ? "🔁 Reservar también para todo el mes (Próximas 4 semanas)" : "🔁 Book also for the whole month (Next 4 weeks)";
    public static string PassengerDash_WeekOf => IsSpanish ? "Semana del" : "Week of";
    public static string PassengerDash_HomeLabel => IsSpanish ? "📍 Ubicación Personal" : "📍 Personal Location";

    // --- FLEET PROFILE ---
    public static string Profile_AboutUs => IsSpanish ? "Sobre Nosotros" : "About Us";
    public static string Profile_OurTeam => IsSpanish ? "Nuestro Equipo" : "Our Team";
    public static string Profile_NoBio => IsSpanish ? "Este chofer aún no ha agregado una descripción a su flota." : "This driver hasn't added a description yet.";
    public static string Profile_ViewSchedulesBtn => IsSpanish ? "📅 Ver Horarios y Reservar" : "📅 View Schedules & Book";

    // --- PASSENGER RULES ---
    public static string Rules_Btn => IsSpanish ? "📖 Guía y Normas de Reserva" : "📖 Booking Guide & Rules";
    public static string Rules_Title => IsSpanish ? "Normas de Convivencia" : "Community Rules";
    public static string Rules_Subtitle => IsSpanish ? "Guía para un viaje exitoso" : "Guide for a successful trip";
    public static string Rules_Intro => IsSpanish ? "PassGold funciona gracias al respeto y la organización entre estudiantes y choferes. Lee estas normas básicas antes de reservar:" : "PassGold works thanks to the respect and organization between students and drivers. Read these basic rules before booking:";
    public static string Rules_1_Title => IsSpanish ? "1. Reserva con Anticipación 📅" : "1. Book in Advance 📅";
    public static string Rules_1_Desc => IsSpanish ? "Organiza tu horario de ciclo. Puedes reservar tus viajes de ida y regreso para todo el mes. Esto ayuda a las flotas a prepararse y evita que te quedes sin asiento en horas pico." : "Organize your semester schedule. You can book your inbound and outbound trips for the whole month. This helps fleets prepare and prevents you from losing a seat during rush hours.";
    public static string Rules_2_Title => IsSpanish ? "2. Cancelaciones Responsables ❌" : "2. Responsible Cancellations ❌";
    public static string Rules_2_Desc => IsSpanish ? "¿El maestro te dejó salir tarde? ¿Te enfermaste? Cancela tu reserva desde la app lo antes posible. Si no cancelas, ese asiento viajará vacío y el chofer perderá ingresos. Sé considerado con la flota y tus compañeros." : "Did the teacher let you out late? Did you get sick? Cancel your booking from the app as soon as possible. If you don't cancel, that seat will travel empty and the driver will lose income. Be considerate of the fleet and your peers.";
    public static string Rules_3_Title => IsSpanish ? "3. Puntualidad Estricta ⏰" : "3. Strict Punctuality ⏰";
    public static string Rules_3_Desc => IsSpanish ? "El microbús tiene un horario que cumplir para que todos lleguen a tiempo a clases. Debes estar en tu punto de recogida asignado antes de la hora indicada. La unidad no puede esperar." : "The minibus has a schedule to keep so everyone gets to class on time. You must be at your assigned pick-up point before the indicated time. The unit cannot wait.";
    public static string Rules_4_Title => IsSpanish ? "4. Exclusividad de Horario 🚫" : "4. Schedule Exclusivity 🚫";
    public static string Rules_4_Desc => IsSpanish ? "El sistema no te permitirá acaparar asientos en diferentes flotas a la misma hora. Si decides cambiar de flota, deberás liberar tu asiento actual primero." : "The system will not allow you to hoard seats in different fleets at the same time. If you decide to change fleets, you must release your current seat first.";

    // --- SETTINGS PAGE (PERFIL) ---
    public static string SettingsPageTitle => IsSpanish ? "Ajustes y Perfil" : "Settings & Profile";
    public static string SettingsInviteTitle => IsSpanish ? "Invitar a mi Flota" : "Invite to my Fleet";
    public static string SettingsInviteDesc => IsSpanish ? "Comparte este código con tus choferes empleados para que se unan a tu flota:" : "Share this code with your employee drivers so they can join your fleet:";
    public static string SettingsCopyBtn => IsSpanish ? "Copiar Código" : "Copy Code";
    public static string SettingsMyFleetTitle => IsSpanish ? "Mi Flota" : "My Fleet";
    public static string SettingsPasteDesc => IsSpanish ? "Pega aquí el código que te envió tu jefe para unirte a su flota:" : "Paste the code your boss sent you here to join their fleet:";
    public static string SettingsInputPlaceholder => IsSpanish ? "Código de Flota" : "Fleet Code";
    public static string SettingsJoinBtn => IsSpanish ? "Vincular Cuenta" : "Link Account";
    public static string SettingsWorkingFor => IsSpanish ? "Trabajando para:" : "Working for:";
    public static string AdminFleet_Title => IsSpanish ? "Administrar Flota" : "Manage Fleet";
    public static string AdminFleet_MyBuses => IsSpanish ? "Mis Microbuses" : "My Buses";
    public static string AdminFleet_AddBtn => IsSpanish ? "+ Agregar" : "+ Add";
    public static string AdminFleet_MyDrivers => IsSpanish ? "Mis Choferes" : "My Drivers";
    public static string AdminFleet_NoEmployees => IsSpanish ? "Aún no tienes choferes empleados." : "You don't have any employee drivers yet.";
    public static string AdminFleet_Instructions => IsSpanish ? "Comparte tu código de invitación con ellos para que se unan." : "Share your invite code with them so they can join.";
    public static string Settings_DescriptionLabel => IsSpanish ? "Descripción de la Flota" : "Fleet Description";
    public static string Settings_DescriptionHint => IsSpanish ? "Cuéntale a los pasajeros sobre tu servicio (años de experiencia, seguridad, puntualidad...)" : "Tell passengers about your service (years of experience, safety, punctuality...)";
    public static string SettingsUpdatedTitle => IsSpanish ? "¡Actualizado!" : "Updated!";
    public static string SettingsUpdatedMsg => IsSpanish ? "Los datos de tu flota ya son visibles para los pasajeros." : "Your fleet's info is now visible to passengers.";
    public static string SettingsSaveError => IsSpanish ? "Hubo un error al intentar guardar la configuración." : "There was an error trying to save the settings.";
    public static string SettingsSessionError => IsSpanish ? "No se pudo verificar tu sesión. Intenta de nuevo." : "Couldn't verify your session. Try again.";
    public static string SettingsLogoutBtn => IsSpanish ? "Cerrar Sesión" : "Log Out";
    public static string SettingsLogisticsTitle => IsSpanish ? "Logística de la Flota" : "Fleet Logistics";
    public static string SettingsLogisticsDesc => IsSpanish ? "Configura las rutas y números de contacto para que los pasajeros te encuentren en el buscador." : "Configure routes and contact numbers so passengers can find you in the search engine.";
    public static string SettingsFleetNameLabel => IsSpanish ? "Nombre Comercial (Alias)" : "Commercial Name (Alias)";
    public static string SettingsFleetNameHint => IsSpanish ? "Ej. Transportes Estudio" : "e.g. Study Transport";
    public static string SettingsPhoneLabel => IsSpanish ? "WhatsApp de Contacto" : "Contact WhatsApp";
    public static string SettingsRoutesLabel => IsSpanish ? "Rutas y Estacionamientos" : "Routes and Parking";
    public static string SettingsRoutesHint => IsSpanish ? "Escribe un lugar y presiona Enter (Ej. UES)" : "Type a place and press Enter (e.g. UES)";
    public static string SettingsSaveLogisticsBtn => IsSpanish ? "Guardar Logística" : "Save Logistics";
    public static string SettingsRegenerateCode => IsSpanish ? "Regenerar Código" : "Regenerate Code";

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

    public static string PassengerDetail_Title => IsSpanish ? "Datos del Pasajero" : "Passenger Details";
    public static string PassengerDetail_Pickup => IsSpanish ? "Punto de recogida" : "Pickup point";
    public static string PassengerDetail_Call => IsSpanish ? "📞 Llamar" : "📞 Call";
    public static string PassengerDetail_NoPhone => IsSpanish ? "Este pasajero no tiene un teléfono registrado." : "This passenger has no phone number on file.";
    public static string PassengerDetail_CallError => IsSpanish ? "No se pudo abrir el marcador telefónico en este dispositivo." : "Couldn't open the phone dialer on this device.";

    // --- MAPA Y PUNTOS DE META ---
    public static string Schedule_MapErrorNoWaypoints => IsSpanish ? "Por favor añade al menos un Punto de Destino/Meta." : "Please add at least one Destination/Waypoint.";
    public static string Schedule_WaypointsTitle => IsSpanish ? "Puntos de Meta (Destinos)" : "Waypoint Destinations";
    public static string Schedule_AddWaypointBtn => IsSpanish ? "+ Añadir destino en mapa" : "+ Add destination on map";
    public static string Schedule_MapModalTitle => IsSpanish ? "Mueve el mapa para ubicar la meta" : "Move the map to set the destination";
    public static string Schedule_MapSearchPlaceholder => IsSpanish ? "Buscar lugar (Ej: UNICAES, Estacionamiento)..." : "Search place (e.g. Campus, Parking)...";
    public static string Schedule_MapAliasPrompt => IsSpanish ? "¿Qué lugar es este?" : "What place is this?";
    public static string Schedule_MapAliasPlaceholder => IsSpanish ? "Ej: UES (Entrada Principal)" : "e.g. Main Entrance";
    public static string Schedule_MapConfirmBtn => IsSpanish ? "Fijar Destino Aquí" : "Set Destination Here";
    public static string Schedule_MapErrorNoName => IsSpanish ? "Por favor ponle un nombre a este lugar." : "Please give this place a name.";
    public static string Global_NotFound => IsSpanish ? "No encontrado" : "Not found";
    public static string Schedule_MapErrorLocation => IsSpanish ? "No pudimos localizar ese lugar exacto. Intenta con un nombre más general, o simplemente arrastra el mapa hasta el lugar correcto." : "We couldn't find that exact location. Try a more general name, or just drag the map to the correct spot.";
    public static string Global_NoInternetSearch => IsSpanish ? "Necesitas internet para usar el buscador de direcciones." : "You need internet to use the address search.";

    // --- MAPA RESERVA PASAJERO ---
    public static string Map_ConfigureTrip => IsSpanish ? "Configura tu viaje" : "Configure your trip";
    public static string Map_MyPickup => IsSpanish ? "🟢 Mi punto de recogida" : "🟢 My pick-up point";
    public static string Map_MyDropoff => IsSpanish ? "🔵 / 🚩 Mi punto de bajada" : "🔵 / 🚩 My drop-off point";
    public static string Map_ConfirmBooking => IsSpanish ? "Confirmar Reserva" : "Confirm Booking";
    public static string Map_TouchOrigin => IsSpanish ? "Toca el mapa para establecer tu origen" : "Tap the map to set your origin";
    public static string Map_TouchDestination => IsSpanish ? "Destino Oficial (o toca el mapa para alterno)" : "Official Destination (or tap for alternate)";
    public static string Map_OriginSet => IsSpanish ? "Origen establecido en el mapa" : "Origin set on the map";
    public static string Map_DestinationSet => IsSpanish ? "Bajada alterna establecida" : "Alternate drop-off set";
    public static string Map_OfficialMeta => IsSpanish ? "🚩 META OFICIAL:" : "🚩 OFFICIAL DESTINATION:";
    public static string PassengerDash_SessionExpired => IsSpanish ? "Tu sesión expiró. Inicia sesión nuevamente." : "Your session expired. Please log in again.";
    public static string PassengerDash_BookingSuccess => IsSpanish ? "¡Asiento reservado con éxito!" : "Seat booked successfully!";
    public static string PassengerDash_BookingError => IsSpanish ? "Hubo un problema procesando la reserva." : "There was a problem processing the booking.";
    public static string PassengerDash_ScheduleClash => IsSpanish ? "Choque de Horario" : "Schedule Clash";
    public static string PassengerDash_AlreadyBookedHour => IsSpanish ? "Ya tienes un asiento reservado a esta misma hora." : "You already have a seat booked at this same time.";
    public static string PassengerDash_Understood => IsSpanish ? "Entendido" : "Understood";
    public static string PassengerDash_OfficialMetaOption => IsSpanish ? "🏁 Meta Oficial del Chofer" : "🏁 Driver's Official Destination";
    public static string PassengerDash_NewMapLocationOption => IsSpanish ? "📍 Elegir nuevo lugar en el mapa" : "📍 Choose a new spot on the map";
    public static string PassengerDash_WhereToDropPrompt => IsSpanish ? "¿Dónde te dejamos en este viaje?" : "Where should we drop you off?";
    public static string PassengerDash_WherePickupPrompt => IsSpanish ? "¿Dónde te recogemos en este viaje?" : "Where should we pick you up?";
    public static string PassengerDash_MixedDirectionError => IsSpanish ? "Seleccionaste viajes de Ida y de Regreso juntos — hacelo por separado, uno primero y otro después." : "You selected both Inbound and Outbound trips together — please book them separately.";
    public static string PassengerDash_NeedHomeFirst => IsSpanish ? "Antes de tu primera reserva, marcá en el mapa tu ubicación personal. Solo lo vas a hacer una vez — después de guardarla, volvé a presionar Reservar." : "Before your first booking, mark your personal location on the map. You'll only do this once — after saving it, tap Book again.";
    public static string SetHome_Title => IsSpanish ? "Guardar Ubicación" : "Save Location";
    public static string SetHome_Instructions => IsSpanish ? "Tocá el mapa en el punto exacto de tu ubicación (o cerca, si vive en un lugar sin acceso directo)." : "Tap the map at the exact spot of your location (or nearby, if it's somewhere without direct access).";
    public static string SetHome_SaveBtn => IsSpanish ? "Guardar Ubicación" : "Save Location";
    public static string SetHome_SavedSuccess => IsSpanish ? "¡Listo! Tu ubicación quedó guardada. Ahora presioná Reservar de nuevo." : "Done! Your location is saved. Now tap Book again.";
    public static string SetHome_NoPinError => IsSpanish ? "Tocá el mapa primero para marcar tu ubicación." : "Tap the map first to mark your location.";
    public static string PassengerDash_ScopePrompt => IsSpanish ? "¿Para cuándo quieres esta reserva?" : "For when do you want this booking?";
    public static string PassengerDash_PastDateError => IsSpanish ? "Ese día ya pasó. Elegí un día de hoy en adelante." : "That day has already passed. Please pick a day from today onward.";
    public static string PassengerDash_ScopeOnlyThisDay => IsSpanish ? "Solo este día" : "Just this day";
    public static string PassengerDash_ScopeThisWeek => IsSpanish ? "Toda esta semana" : "All this week";
    public static string PassengerDash_ScopeThisMonth => IsSpanish ? "Este mes" : "This month";
    public static string PassengerDash_ScopeRestOfCycle => IsSpanish ? "Resto del ciclo" : "Rest of the cycle";
    public static string PassengerDash_ScopeCustomRange => IsSpanish ? "📅 Elegir hasta qué fecha" : "📅 Choose an end date";
    public static string ConfirmarReserva_RangoHastaLabel => IsSpanish ? "Repetir hasta:" : "Repeat until:";
    public static string PassengerDash_StableConnectionTitle => IsSpanish ? "Conexión estable recomendada" : "Stable connection recommended";
    public static string PassengerDash_StableConnectionMsg => IsSpanish ? "Vas a reservar varios días de golpe. Te recomendamos hacer esto con buena conexión a internet para que no se quede a medias." : "You're about to book several days at once. We recommend doing this with a good internet connection so it doesn't get stuck halfway.";
    public static string PassengerDash_ContinueAnyway => IsSpanish ? "Continuar" : "Continue";
    public static string PassengerDash_NeedInternetToBook => IsSpanish ? "Necesitas internet para hacer una reserva." : "You need internet to book a seat.";
    public static string PassengerDash_BulkBookingSuccess => IsSpanish ? "Se reservaron {0} de {1} viajes." : "{0} of {1} trips were booked.";
    public static string Global_Next => IsSpanish ? "Siguiente" : "Next";
    public static string Map_PickUpLabel => IsSpanish ? "🟢 RECOGIDA" : "🟢 PICK-UP";
    public static string Map_DropOffLabel => IsSpanish ? "🔵 BAJADA ALTERNA" : "🔵 ALTERNATE DROP-OFF";

    // --- ALERTAS Y DIRECCIONES DEL DASHBOARD ---
    public static string Alert_CancelTrip => IsSpanish ? "Anular Viaje" : "Cancel Trip";
    public static string Alert_CancelConfirm => IsSpanish ? "¿Estás seguro de que deseas anular tu espacio en esta unidad?" : "Are you sure you want to cancel your seat in this unit?";
    public static string Alert_YesCancel => IsSpanish ? "Sí, anular" : "Yes, cancel";
    public static string Alert_GoBack => IsSpanish ? "Volver" : "Go back";
    public static string Alert_NeedInternetCancel => IsSpanish ? "Necesitas internet para anular una reserva." : "You need internet to cancel a booking.";
    public static string Alert_CancelSuccess => IsSpanish ? "Reserva anulada correctamente." : "Booking cancelled successfully.";
    public static string Alert_CancelError => IsSpanish ? "No pudimos procesar la anulación." : "We couldn't process the cancellation.";
    public static string Alert_SelectLocation => IsSpanish ? "Selecciona una ubicación guardada o añade una nueva." : "Select a saved location or add a new one.";
    public static string Alert_EnterExactIndication => IsSpanish ? "Por favor ingresa la indicación exacta de recogida." : "Please enter the exact pick-up indication.";
    public static string Alert_SeatBooked => IsSpanish ? "¡Asiento Reservado con éxito!" : "Seat successfully booked!";
    public static string Alert_BookingProblem => IsSpanish ? "Hubo un problema procesando la reserva." : "There was a problem processing the booking.";
    public static string Alert_AppFailed => IsSpanish ? "La app falló al procesar: {0}" : "The app failed to process: {0}";
    public static string Dash_ToUniversity => IsSpanish ? "Hacia la U" : "To Campus";
    public static string Dash_ToHome => IsSpanish ? "Hacia Casa" : "To Home";
    public static string Dash_LocalFleet => IsSpanish ? "Flota Local" : "Local Fleet";
    public static string TripDetails_NoPlate => IsSpanish ? "Sin Placa" : "No Plate";
    public static string TripDetails_DriverMeta => IsSpanish ? "Meta del chofer" : "Driver's meta point";
    public static string TripDetails_PickUpPrefix => IsSpanish ? "Recoger" : "Pick up";
    public static string TripDetails_DropOffPrefix => IsSpanish ? "Bajar" : "Drop off";
    public static string ActiveTrip_Incomplete => IsSpanish ? "Viaje Incompleto" : "Incomplete Trip";
    public static string ActiveTrip_MissingStudents => IsSpanish ? "Aún faltan {0} estudiantes por subir. ¿Seguro que deseas finalizar?" : "{0} students haven't boarded yet. Are you sure you want to finish?";
    public static string ActiveTrip_FinishConfirmTitle => IsSpanish ? "¿Finalizar el viaje?" : "Finish the trip?";
    public static string ActiveTrip_FinishConfirmBody => IsSpanish ? "Se dejará de compartir tu ubicación con los pasajeros." : "Your location will stop being shared with passengers.";
    public static string ActiveTrip_YesFinish => IsSpanish ? "Sí, Finalizar" : "Yes, Finish";
    public static string Global_Cancel => IsSpanish ? "Cancelar" : "Cancel";
    public static string Map_LegendMeta => IsSpanish ? "Meta del chofer" : "Driver's destination";
    public static string Map_LegendPickup => IsSpanish ? "Tu recogida" : "Your pickup";
    public static string Map_LegendDropoff => IsSpanish ? "Tu bajada" : "Your dropoff";
    public static string TripDetails_TabPickup => IsSpanish ? "📍 Recogida" : "📍 Pickup";
    public static string TripDetails_TabDropoff => IsSpanish ? "🏁 Bajada" : "🏁 Dropoff";
    public static string Map_PromptPickupIda => IsSpanish ? "¿Dónde te recogen?" : "Where do they pick you up?";
    public static string Map_PromptPickupRegreso => IsSpanish ? "¿Dónde abordas (en la U)?" : "Where do you board (at campus)?";
    public static string Map_PromptDropoffIda => IsSpanish ? "¿Dónde te dejan (cerca de la U)?" : "Where do they drop you off (near campus)?";
    public static string Map_PromptDropoffRegreso => IsSpanish ? "¿Dónde te dejan (en casa)?" : "Where do they drop you off (at home)?";

    // --- SETTINGS: EDICIÓN DE PERFIL ---
    public static string SettingsProfile_NeedInternet => IsSpanish ? "Necesitas internet para hacer esto." : "You need internet to do this.";
    public static string SettingsProfile_UploadError => IsSpanish ? "No se pudo subir la foto. Intenta de nuevo." : "Couldn't upload the photo. Try again.";
    public static string SettingsProfile_NameEmptyError => IsSpanish ? "El nombre no puede estar vacío." : "Name can't be empty.";
    public static string SettingsProfile_SaveSuccessTitle => IsSpanish ? "¡Guardado!" : "Saved!";
    public static string SettingsProfile_SaveSuccessMsg => IsSpanish ? "Tu perfil se actualizó correctamente." : "Your profile was updated successfully.";
    public static string SettingsProfile_SaveError => IsSpanish ? "No se pudo guardar tu perfil. Intenta de nuevo." : "Couldn't save your profile. Try again.";
    public static string SettingsEnterCodeError => IsSpanish ? "Ingresa el código que te dio tu jefe." : "Enter the code your boss gave you.";
    public static string SettingsJoinWelcomeTitle => IsSpanish ? "¡Bienvenido!" : "Welcome!";
    public static string SettingsJoinErrorTitle => IsSpanish ? "Error de vinculación" : "Linking error";
    public static string SettingsJoinErrorClose => IsSpanish ? "Cerrar" : "Close";
    public static string SettingsLogoutConfirmTitle => IsSpanish ? "Cerrar Sesión" : "Log Out";
    public static string SettingsLogoutConfirmMsg => IsSpanish ? "¿Estás seguro que deseas salir de tu cuenta?" : "Are you sure you want to log out?";
    public static string SettingsLogoutConfirmYes => IsSpanish ? "Sí, salir" : "Yes, log out";
    public static string SettingsLogoutConfirmCancel => IsSpanish ? "Cancelar" : "Cancel";
    public static string TripDetails_SearchPassengerPrompt => IsSpanish ? "Buscar pasajero o dirección..." : "Search passenger or address...";
    public static string SettingsProfile_EditBtn => IsSpanish ? "✏️ Editar" : "✏️ Edit";
    public static string SettingsProfile_ChangePhotoBtn => IsSpanish ? "📷 Cambiar Foto" : "📷 Change Photo";
    public static string SettingsProfile_NameLabel => IsSpanish ? "Nombre completo" : "Full name";
    public static string SettingsProfile_PhoneLabel => IsSpanish ? "Teléfono" : "Phone";
    public static string SettingsProfile_SaveBtn => IsSpanish ? "💾 Guardar" : "💾 Save";
    public static string SettingsProfile_CancelBtn => IsSpanish ? "Cancelar" : "Cancel";

    // --- CONFIRMAR RESERVA (NUEVO) ---
    public static string ConfirmarReserva_Title => IsSpanish ? "Confirmar Reserva" : "Confirm Booking";
    public static string ConfirmarReserva_TocandoCasaHint => IsSpanish ? "🟢 Tocá el mapa para ubicar TU PUNTO PERSONAL" : "🟢 Tap the map to locate YOUR PERSONAL POINT";
    public static string ConfirmarReserva_TocandoAlternoHint => IsSpanish ? "🔵 Tocá el mapa para el punto alterno" : "🔵 Tap the map for the alternate point";
    public static string ConfirmarReserva_CasaFijaDesc => IsSpanish ? "Ubicación personal: Tu colonia, residencial o punto exacto." : "Personal location: Your neighborhood, residential area, or exact point.";
    public static string ConfirmarReserva_TocarCasaBtn => IsSpanish ? "📍 Tocar el mapa para marcar/mover mi punto" : "📍 Tap the map to set/move my point";
    public static string ConfirmarReserva_ParadasOficialesLabel => IsSpanish ? "Paradas oficiales del chofer:" : "Driver's official stops:";
    public static string ConfirmarReserva_MisUbicacionesLabel => IsSpanish ? "Tus ubicaciones guardadas:" : "Your saved locations:";
    public static string ConfirmarReserva_PinNuevoBtn => IsSpanish ? "📍 O tocar el mapa para un pin nuevo" : "📍 Or tap the map for a new pin";
    public static string ConfirmarReserva_GuardarNuevoLabel => IsSpanish ? "Guardar este lugar con un nombre para la próxima vez" : "Save this place with a name for next time";
    public static string ConfirmarReserva_AliasPlaceholder => IsSpanish ? "Ej. Trabajo, Edificio I..." : "E.g. Work, Building I...";
    public static string ConfirmarReserva_ElegidoLabel => IsSpanish ? "Elegido:" : "Chosen:";
    public static string ConfirmarReserva_ParaCuandoLabel => IsSpanish ? "¿Para cuándo?" : "For when?";
    public static string ConfirmarReserva_AlcancePickerTitle => IsSpanish ? "Elegí el alcance" : "Choose the scope";
    public static string ConfirmarReserva_ConfirmarBtn => IsSpanish ? "✅ Confirmar Reserva" : "✅ Confirm Booking";
    public static string ConfirmarReserva_Ubicando => IsSpanish ? "Ubicando..." : "Locating...";
    public static string ConfirmarReserva_PinSinDireccion => IsSpanish ? "📍 Punto marcado en el mapa" : "📍 Point marked on the map";
    public static string ConfirmarReserva_TabRecogida => IsSpanish ? "🟢 Recogida" : "🟢 Pickup";
    public static string ConfirmarReserva_TabBajada => IsSpanish ? "🔵 Bajada" : "🔵 Drop-off";
    public static string ConfirmarReserva_MultipleDrivers => IsSpanish ? "Múltiples Choferes (Rotación)" : "Multiple Drivers (Rotation)";
    public static string ConfirmarReserva_MultipleUnits => IsSpanish ? "Varias Unidades" : "Various Units";
    public static string ConfirmarReserva_DescRequiredTitle => IsSpanish ? "Detalle requerido" : "Detail required";
    public static string ConfirmarReserva_NeedDescriptionError => IsSpanish ? "Escribe una descripción clara de tu ubicación (Ej. Colonia San Rafael) para que el chofer pueda ubicarte fácilmente." : "Write a clear description of your location (e.g. San Rafael Neighborhood) so the driver can easily find you.";
    public static string ConfirmarReserva_LocRequiredError => IsSpanish ? "Por favor ubica tu punto personal en el mapa." : "Please locate your personal point on the map.";
    public static string PassengerDash_ScopeCurrentSelection => IsSpanish ? "Solo la selección actual" : "Only current selection";

    public static string EditTrip_ConfirmDeleteAll => IsSpanish ? "¿Seguro que deseas eliminar TODA la serie de este viaje de forma permanente?" : "Are you sure you want to permanently delete the ENTIRE series for this trip?";
    public static string EditTrip_ConfirmDeleteRange => IsSpanish ? "¿Seguro que deseas eliminar los viajes desde el {0} hasta el {1}?" : "Are you sure you want to delete trips from {0} to {1}?";
    public static string Schedule_MapDefaultLocation => IsSpanish ? "Ubicación en mapa" : "Map location";
    public static string Schedule_MapDetectedLocation => IsSpanish ? "Ubicación detectada" : "Detected location";

    // ===== BEGIN TEXTOS NUEVOS (antes estaban quemados en XAML / C#) =====
    public static string ActiveTrip_StartBtn => IsSpanish ? "▶ Iniciar Viaje" : "▶ Start Trip";
    public static string AdminFleet_PhonePlaceholder => IsSpanish ? "Ej. 70001234" : "E.g. 70001234";
    public static string AdminFleet_RegenerateBtn => IsSpanish ? "🔄 Regenerar" : "🔄 Regenerate";
    public static string AdminFleet_CapacityFormat => IsSpanish ? "Capacidad: {0}" : "Capacity: {0}";
    public static string Confirm_HomeHint => IsSpanish ? "En la ida te recoge acá; en el regreso te deja acá." : "On the way to campus you're picked up here; on the way back you're dropped off here.";
    public static string Confirm_AddressLabel => IsSpanish ? "Dirección y referencia (el chofer la va a leer)" : "Address and landmark (the driver will read it)";
    public static string Confirm_AddressPlaceholder => IsSpanish ? "Ej.: Col. San Rafael, pasaje 3, portón negro" : "E.g.: Col. San Rafael, passage 3, black gate";
    public static string Confirm_AddressWarning => IsSpanish ? "No pongas solo \"Mi casa\": el chofer ve la lista de todos sus pasajeros y necesita saber dónde parar." : "Don't just write \"My home\": the driver sees the list of all their passengers and needs to know where to stop.";
    public static string Confirm_AltHint => IsSpanish ? "En la ida te deja acá; en el regreso te recoge acá. Puede ser la parada del chofer u otro punto en el camino (ej. Metrocentro)." : "On the way to campus you're dropped off here; on the way back you're picked up here. It can be the driver's stop or another point along the way (e.g. Metrocentro).";
    public static string Confirm_OfficialPointsLabel => IsSpanish ? "Puntos oficiales del chofer:" : "Driver's official points:";
    public static string Confirm_ChosenPrefix => IsSpanish ? "Elegido: " : "Chosen: ";
    public static string Confirm_SavedPlacesHint => IsSpanish ? "Cuando reserves, tus lugares quedan guardados acá para la próxima vez." : "When you book, your places are saved here for next time.";
    public static string Confirm_SummaryTitle => IsSpanish ? "Así queda tu viaje" : "Your trip at a glance";
    public static string Confirm_InboundLabel => IsSpanish ? "🏫 Ida (llegar a la U)" : "🏫 Inbound (arrive at campus)";
    public static string Confirm_OutboundLabel => IsSpanish ? "🏠 Regreso (volver a casa)" : "🏠 Return (go home)";
    public static string Confirm_RepeatTitle => IsSpanish ? "¿Repetir cada semana?" : "Repeat every week?";
    public static string Confirm_RepeatDesc => IsSpanish ? "Se reservan los mismos días y horas que elegiste, semana tras semana." : "The same days and times you picked are booked, week after week.";
    public static string Confirm_RepeatPickerTitle => IsSpanish ? "Elegí una opción" : "Choose an option";
    public static string Confirm_RepeatUntil => IsSpanish ? "Repetir hasta el:" : "Repeat until:";
    public static string Fmt_DayMonthLong => IsSpanish ? "dddd d 'de' MMMM" : "dddd, MMMM d";
    public static string Driver_EditBtn => IsSpanish ? "✏️ Editar" : "✏️ Edit";
    public static string Driver_DetailBtn => IsSpanish ? "Detalle" : "Details";
    public static string Driver_PassengersFormat => IsSpanish ? "{0} Pasajeros" : "{0} Passengers";
    public static string EditTrip_Title => IsSpanish ? "Editar Programación" : "Edit Schedule";
    public static string EditTrip_ReadOnly => IsSpanish ? "🔒 Solo Lectura" : "🔒 Read Only";
    public static string EditTrip_ReadOnlyDesc => IsSpanish ? "Solo el Chofer Jefe o Independiente puede modificar o cancelar viajes programados." : "Only the Fleet Owner or an Independent Driver can modify or cancel scheduled trips.";
    public static string EditTrip_TripType => IsSpanish ? "Clasificación del Viaje" : "Trip Type";
    public static string EditTrip_ChooseFrequent => IsSpanish ? "📍 Elegir destino frecuente guardado..." : "📍 Choose a saved frequent destination...";
    public static string EditTrip_ScopeQuestion => IsSpanish ? "¿Para qué fechas aplica este cambio?" : "Which dates does this change apply to?";
    public static string EditTrip_StartFrom => IsSpanish ? "Comenzar desde:" : "Start from:";
    public static string EditTrip_UntilDay => IsSpanish ? "Hasta el día:" : "Until:";
    public static string EditTrip_OfficialDeparture => IsSpanish ? "Hora Oficial de Salida" : "Official Departure Time";
    public static string EditTrip_RealStart => IsSpanish ? "Inicio real" : "Real start";
    public static string EditTrip_UnitsTitle => IsSpanish ? "Unidades de este horario" : "Units for this time slot";
    public static string EditTrip_RemoveUnit => IsSpanish ? "✕ Quitar" : "✕ Remove";
    public static string EditTrip_AddUnit => IsSpanish ? "🚐 Añadir otra unidad a este horario" : "🚐 Add another unit to this time slot";
    public static string Global_SaveChanges => IsSpanish ? "Guardar Cambios" : "Save Changes";
    public static string EditTrip_DeleteSlot => IsSpanish ? "🗑️ Eliminar horario completo" : "🗑️ Delete entire time slot";
    public static string EditTrip_DeleteSlotHint => IsSpanish ? "Elimina todas las unidades de este horario. Para quitar solo una, usá “✕ Quitar” en esa unidad y luego “Guardar Cambios”." : "Deletes every unit in this time slot. To remove just one, use “✕ Remove” on that unit and then “Save Changes”.";
    public static string Directory_SearchPlaceholder => IsSpanish ? "Buscar flotas, ej: Santa Ana..." : "Search fleets, e.g. Santa Ana...";
    public static string FleetSched_Title => IsSpanish ? "Elegí días y horarios" : "Pick days and times";
    public static string FleetSched_Subtitle => IsSpanish ? "Podés marcar varios días, también de las próximas semanas." : "You can select several days, including upcoming weeks.";
    public static string FleetSched_ArriveHeader => IsSpanish ? "🏫 Llegar a la U" : "🏫 Arrive at campus";
    public static string FleetSched_NoArrivals => IsSpanish ? "Sin llegadas" : "No arrivals";
    public static string FleetSched_LeaveHeader => IsSpanish ? "🏠 Salir de la U" : "🏠 Leave campus";
    public static string FleetSched_NoDepartures => IsSpanish ? "Sin salidas" : "No departures";
    public static string FleetSched_DayHint => IsSpanish ? "✅ = ya lo tenés reservado.  Deslizá los días a la derecha para ver las próximas semanas." : "✅ = already booked.  Swipe the days sideways to see upcoming weeks.";
    public static string FleetSched_Legend => IsSpanish ? "🏫 llegar a la U · 🏠 salir de la U · ✅ reservado" : "🏫 arrive at campus · 🏠 leave campus · ✅ booked";
    public static string FleetSched_TimeHeader => IsSpanish ? "Hora" : "Time";
    public static string FleetSched_NoTripsWeek => IsSpanish ? "No hay viajes esta semana." : "No trips this week.";
    public static string FleetSched_GridHint => IsSpanish ? "Tocá una celda para marcarla. ×2 = salen 2 micros: te pregunta con qué chofer ir. Girá el celular para ver más columnas." : "Tap a cell to select it. ×2 = 2 buses leave: you'll be asked which driver to ride with. Rotate your phone to see more columns.";
    public static string Login_QuickWith => IsSpanish ? "O ingresa rápidamente con" : "Or sign in quickly with";
    public static string Track_Title => IsSpanish ? "Rastreo de Unidad" : "Unit Tracking";
    public static string Track_NoTrips => IsSpanish ? "Sin Viajes en Curso" : "No Trips in Progress";
    public static string Track_NoTripsDesc => IsSpanish ? "Cuando tengas una reserva dentro de su horario de recorrido, el mapa aparecerá aquí automáticamente." : "When you have a booking within its route window, the map will appear here automatically.";
    public static string Track_UnitOnTheWay => IsSpanish ? "Unidad en Camino" : "Unit on the Way";
    public static string Track_Btn => IsSpanish ? "📍 Rastrear Unidad" : "📍 Track Unit";
    public static string PDash_Title => IsSpanish ? "Mi Panel" : "My Dashboard";
    public static string PDash_MySchedule => IsSpanish ? "📅 Mi Horario" : "📅 My Schedule";
    public static string PDash_SearchFleets => IsSpanish ? "🔍 Buscar Flotas" : "🔍 Find Fleets";
    public static string PDash_ActiveWeek => IsSpanish ? "Semana Activa" : "Active Week";
    public static string PDash_NoTripsYet => IsSpanish ? "Aún no tienes viajes programados." : "You don't have any scheduled trips yet.";
    public static string PDash_TripInProgress => IsSpanish ? "🚀 Viaje en Curso" : "🚀 Trip in Progress";
    public static string PDash_CancelTripBtn => IsSpanish ? "✖ Anular Viaje" : "✖ Cancel Trip";
    public static string SetHome_SearchPlaceholder => IsSpanish ? "Buscar dirección o punto de referencia..." : "Search address or landmark...";
    public static string TripDetails_AllFleet => IsSpanish ? "Toda la flota desplegada en este horario:" : "All units deployed at this time:";
    public static string Global_NoInternetTitle => IsSpanish ? "Sin conexión a Internet" : "No Internet connection";
    public static string TripDetails_NoInternetMap => IsSpanish ? "Conéctate a Wi-Fi o datos para ver el mapa interactivo." : "Connect to Wi-Fi or mobile data to see the interactive map.";
    public static string Global_OfflineMode => IsSpanish ? "Modo Fuera de Línea" : "Offline Mode";
    public static string TripDetails_OfflineMapDesc => IsSpanish ? "Conéctate a Internet para visualizar el mapa de ubicaciones." : "Connect to the Internet to view the locations map.";
    public static string TripDetails_CapacityMaxFormat => IsSpanish ? "Capacidad Max: {0} personas" : "Max capacity: {0} people";
    public static string Fleet_DefaultName => IsSpanish ? "Transportes {0}" : "{0} Transport";
    public static string Fleet_RoutesToAgree => IsSpanish ? "Rutas a convenir" : "Routes by arrangement";
    public static string Global_Done => IsSpanish ? "Listo" : "Done";
    public static string Global_DoneExcl => IsSpanish ? "¡Listo!" : "Done!";
    public static string Global_Yes => IsSpanish ? "Sí" : "Yes";
    public static string Global_No => IsSpanish ? "No" : "No";
    public static string Global_Confirm => IsSpanish ? "Confirmar" : "Confirm";
    public static string Global_Success => IsSpanish ? "Éxito" : "Success";
    public static string Global_Loading => IsSpanish ? "Cargando..." : "Loading...";
    public static string Global_NoServerConnection => IsSpanish ? "Sin conexión con el servidor." : "No connection to the server.";
    public static string PDash_SavedLocationAlias => IsSpanish ? "Ubicación Guardada" : "Saved Location";
    public static string PDash_CantOpenPhone => IsSpanish ? "No se pudo abrir el teléfono. Número: +{0}" : "Couldn't open the phone app. Number: +{0}";
    public static string PDash_CantOpenWhatsApp => IsSpanish ? "No se pudo abrir WhatsApp. Número: +{0}" : "Couldn't open WhatsApp. Number: +{0}";
    public static string PDash_TripLoadError => IsSpanish ? "Error al cargar datos del viaje." : "Error loading trip data.";
    public static string PDash_NotYetTitle => IsSpanish ? "Aún no" : "Not yet";
    public static string PDash_TrackingNotYet => IsSpanish ? "El rastreo de la unidad estará activo durante su ventana de recorrido, a partir de las {0}." : "Unit tracking will be active during its route window, starting at {0}.";
    public static string PDash_TripEndedTitle => IsSpanish ? "Viaje finalizado" : "Trip finished";
    public static string PDash_TripEndedMsg => IsSpanish ? "El tiempo de este recorrido ha concluido. El rastreo ya no está disponible." : "This trip's time window has ended. Tracking is no longer available.";
    public static string PDash_PrivacyTitle => IsSpanish ? "Privacidad" : "Privacy";
    public static string PDash_PrivacyMsg => IsSpanish ? "Para los viajes de regreso desde el punto de partida oficial, debes esperar en el microbús. El mapa no está disponible para esta ubicación." : "For return trips from the official departure point, please wait at the bus. The map isn't available for this location.";
    public static string Auth_CredentialsLost => IsSpanish ? "Error: El correo o contraseña se perdieron en la memoria." : "Error: the email or password was lost from memory.";
    public static string Auth_GoogleSignedIn => IsSpanish ? "Sesión iniciada con Google" : "Signed in with Google";
    public static string Auth_AndroidContextError => IsSpanish ? "No se pudo obtener el contexto de Android." : "Couldn't get the Android context.";
    public static string Auth_SupabaseSessionError => IsSpanish ? "No se pudo crear la sesión en Supabase." : "Couldn't create the Supabase session.";
    public static string Auth_CancelledByUser => IsSpanish ? "Operación cancelada por el usuario." : "Operation cancelled by the user.";
    public static string Auth_GoogleFailed => IsSpanish ? "Falló la conexión con Google -> {0}" : "Google connection failed -> {0}";
    public static string Auth_InternalError => IsSpanish ? "Error interno: {0}" : "Internal error: {0}";
    public static string Auth_UseUniversalLogin => IsSpanish ? "Use Universal Login para iOS" : "Use Universal Login on iOS";
    public static string Auth_LinkError => IsSpanish ? "Error al generar enlace de autenticación." : "Error generating the authentication link.";
    public static string Auth_NoValidCredentials => IsSpanish ? "No se recibieron credenciales válidas." : "No valid credentials were received.";
    public static string Auth_SignInCancelled => IsSpanish ? "Inicio de sesión cancelado." : "Sign-in cancelled.";
    public static string Fleet_InvalidCode => IsSpanish ? "El código ingresado no es válido." : "The code you entered isn't valid.";
    public static string Global_NoDataToSave => IsSpanish ? "Sin datos para guardar" : "No data to save";
    public static string Global_NoStopsToSave => IsSpanish ? "Sin paradas para guardar" : "No stops to save";
    public static string Upload_InvalidPath => IsSpanish ? "Ruta de archivo inválida o no existe." : "Invalid or missing file path.";
    public static string Upload_NoUrl => IsSpanish ? "Respuesta sin URL de la función." : "The function's response had no URL.";
    public static string Upload_Timeout => IsSpanish ? "Tiempo de espera agotado (revisa conexión a internet)." : "Request timed out (check your internet connection).";
    public static string Upload_UnreadableImage => IsSpanish ? "No se pudo leer la imagen (formato no soportado o archivo dañado)." : "Couldn't read the image (unsupported format or damaged file).";
    public static string LiveTrack_StatusScheduled => IsSpanish ? "⏳ Empieza a compartir a las {0}" : "⏳ Starts sharing at {0}";
    public static string Dashboard_SharingBanner => IsSpanish ? "📡 Compartiendo tu ubicación · {0}" : "📡 Sharing your location · {0}";
    public static string Dashboard_SharingOffBanner => IsSpanish ? "📴 Tu ubicación no se está compartiendo. Toca aquí para activarla." : "📴 Your location isn't being shared. Tap here to turn it on.";
    public static string Dashboard_SharingHint => IsSpanish ? "Deja la app abierta mientras manejas. Toca para ver el mapa y tus pasajeros." : "Keep the app open while driving. Tap to see the map and your passengers.";
    public static string Dashboard_FinishBtn => IsSpanish ? "■ Finalizar" : "■ Finish";
    public static string LiveTrack_StatusWaitingGps => IsSpanish ? "📡 Buscando tu ubicación…" : "📡 Getting your location…";
    public static string LiveTrack_StatusNoGps => IsSpanish ? "⚠️ El GPS no responde" : "⚠️ GPS not responding";
    public static string LiveTrack_StatusSent => IsSpanish ? "✅ Ubicación enviada a las {0}" : "✅ Location sent at {0}";
    public static string LiveTrack_StatusFailed => IsSpanish ? "❌ No se pudo enviar: {0}" : "❌ Couldn't send: {0}";
    public static string LiveTrack_StatusNoSession => IsSpanish ? "sin sesión, cierra sesión y vuelve a entrar" : "no session, sign out and sign in again";
    public static string LiveTrack_StatusOff => IsSpanish ? "📴 Tu ubicación no se está compartiendo" : "📴 Your location is not being shared";
    public static string LiveTrack_ShareOffTitle => IsSpanish ? "Tu ubicación no se está compartiendo" : "Your location is not being shared";
    public static string LiveTrack_ShareOffBody => IsSpanish ? "La opción \"Compartir mi ubicación\" está apagada. Sin ella, tus pasajeros no te ven en el mapa ni reciben el aviso de que vas cerca. ¿La activo?" : "\"Share my location\" is turned off. Without it, your passengers can't see you on the map or get the bus-nearby alert. Turn it on?";
    public static string LiveTrack_ShareOffYes => IsSpanish ? "Sí, compartir" : "Yes, share";
    public static string LiveTrack_ShareOffNo => IsSpanish ? "No, iniciar sin compartir" : "No, start without sharing";
    public static string LiveTrack_NeedPermission => IsSpanish ? "Para compartir tu ubicación con tus pasajeros, PassGold necesita el permiso de ubicación." : "To share your location with your passengers, PassGold needs location permission.";
    public static string LiveTrack_GpsError => IsSpanish ? "No se pudo leer tu ubicación. Revisá que el GPS del celular esté activado." : "Couldn't read your location. Make sure your phone's GPS is on.";
    public static string LiveTrack_NoServer => IsSpanish ? "Sin conexión con el servidor: tus pasajeros no están viendo tu ubicación." : "No connection to the server: your passengers can't see your location.";
    public static string App_UnexpectedCloseBody => IsSpanish ? "Algo salió mal. Si vuelve a pasar, avísale al administrador de la app." : "Something went wrong. If it happens again, let the app administrator know.";
    public static string App_UnexpectedCloseTitle => IsSpanish ? "Se detectó un cierre inesperado la última vez" : "The app closed unexpectedly last time";
    public static string Track_YourRidePin => IsSpanish ? "Tu transporte" : "Your ride";
    public static string Schedule_PlaceNotFound => IsSpanish ? "Lugar no localizado. Intenta con un nombre más general." : "Place not found. Try a more general name.";
    public static string Schedule_NeedInternetSearch => IsSpanish ? "Necesitas internet para buscar direcciones." : "You need internet to search addresses.";
    public static string ActiveTrip_MapPlaceholder => IsSpanish ? "Mapa del recorrido" : "Route map";
    public static string ActiveTrip_Pickup => IsSpanish ? "Recogida" : "Pick-up";
    public static string ActiveTrip_AltDropoff => IsSpanish ? "Bajada alterna" : "Alt. drop-off";
    public static string ActiveTrip_Destination => IsSpanish ? "Destino" : "Destination";
    public static string ActiveTrip_ViewAll => IsSpanish ? "Ver todos" : "View all";
    public static string ActiveTrip_FinishBtn => IsSpanish ? "■ Finalizar Viaje" : "■ Finish Trip";
    public static string Auth_GoogleEmptyToken => IsSpanish ? "Token vacío devuelto por Google." : "Google returned an empty token.";
    public static string Auth_ExceptionPrefix => IsSpanish ? "Excepción: {0}" : "Exception: {0}";
    public static string Driver_BadgeInbound => IsSpanish ? "IDA" : "IN";
    public static string Driver_BadgeOutbound => IsSpanish ? "REG" : "OUT";
    public static string Confirm_MapHintHome => IsSpanish ? "📍 Tocá el mapa en tu dirección (donde te recoge / deja el micro)" : "📍 Tap the map at your address (where the bus picks you up / drops you off)";
    public static string Confirm_MapHintAlt => IsSpanish ? "🚩 Tocá el mapa si te bajás / subís en otro punto" : "🚩 Tap the map if you get off / on at another point";
    public static string Confirm_PlacesHome => IsSpanish ? "🏠 Lugares de casa que ya usaste:" : "🏠 Home places you've used:";
    public static string Confirm_PlacesAlt => IsSpanish ? "📍 Puntos alternos que ya usaste:" : "📍 Alternate points you've used:";
    public static string Confirm_TabHome => IsSpanish ? "📍 Mi dirección" : "📍 My address";
    public static string Confirm_TabAlt => IsSpanish ? "🚩 Oficial / alterno" : "🚩 Official / alternate";
    public static string Confirm_MissingAddressText => IsSpanish ? "(falta escribir la dirección)" : "(address not written yet)";
    public static string Confirm_MissingAddress => IsSpanish ? "(falta tu dirección)" : "(your address is missing)";
    public static string Confirm_MissingPoint => IsSpanish ? "(falta el punto)" : "(point missing)";
    public static string Confirm_MirrorPickup => IsSpanish ? "🟢 Recoge: {0}" : "🟢 Pick-up: {0}";
    public static string Confirm_MirrorDropoff => IsSpanish ? "🔵 Deja: {0}" : "🔵 Drop-off: {0}";
    public static string Confirm_ScopeOnlyChosen => IsSpanish ? "Solo los días que elegí" : "Only the days I picked";
    public static string Confirm_ScopeOneMonth => IsSpanish ? "Repetir cada semana por 1 mes" : "Repeat weekly for 1 month";
    public static string Confirm_ScopeCycle => IsSpanish ? "Repetir cada semana todo el ciclo" : "Repeat weekly for the whole term";
    public static string Confirm_ScopeCustom => IsSpanish ? "📅 Repetir cada semana hasta…" : "📅 Repeat weekly until…";
    public static string Confirm_SummaryOne => IsSpanish ? "Vas a reservar 1 viaje:" : "You're about to book 1 trip:";
    public static string Confirm_SummaryMany => IsSpanish ? "Vas a reservar {0} viajes:" : "You're about to book {0} trips:";
    public static string Confirm_SummaryLegend => IsSpanish ? "🏫 = llegar a la U   🏠 = volver a casa" : "🏫 = arrive at campus   🏠 = go home";
    public static string Confirm_OfficialPointDefault => IsSpanish ? "Punto oficial" : "Official point";
    public static string Confirm_MyHomePin => IsSpanish ? "Mi casa" : "My home";
    public static string Confirm_NoTripsChosen => IsSpanish ? "No hay viajes elegidos." : "No trips selected.";
    public static string Confirm_MarkAddress => IsSpanish ? "Marcá en el mapa tu dirección (donde te recoge o te deja el micro), o elegí una de las que ya usaste." : "Mark your address on the map (where the bus picks you up or drops you off), or choose one you've used before.";
    public static string Confirm_WriteAddressTitle => IsSpanish ? "Escribí tu dirección" : "Write your address";
    public static string Confirm_WriteAddressMsg => IsSpanish ? "El chofer ve la lista de todos sus pasajeros: \"Mi casa\" no le dice dónde parar.\n\nEscribí la colonia o calle y una referencia. Ej.:\n\"Col. San Rafael, pasaje 3, portón negro\"" : "The driver sees the list of all their passengers: \"My home\" doesn't tell them where to stop.\n\nWrite the neighborhood or street and a landmark. E.g.:\n\"Col. San Rafael, passage 3, black gate\"";
    public static string Confirm_ChooseAltPoint => IsSpanish ? "Elegí el punto oficial del chofer o marcá en el mapa dónde te bajás / subís (ej. Metrocentro)." : "Choose the driver's official point or mark on the map where you get off / on (e.g. Metrocentro).";
    public static string Confirm_RepeatDatePassed => IsSpanish ? "La fecha de \"repetir hasta\" ya pasó." : "The \"repeat until\" date has already passed.";
    public static string Confirm_BookedOne => IsSpanish ? "¡Listo! Reservaste 1 viaje." : "Done! You booked 1 trip.";
    public static string Confirm_BookedMany => IsSpanish ? "¡Listo! Reservaste {0} viajes." : "Done! You booked {0} trips.";
    public static string Confirm_AlreadyHadSome => IsSpanish ? "{0} ya los tenías reservados (no se duplicaron)." : "{0} were already booked (no duplicates were created).";
    public static string Confirm_BookingConfirmedTitle => IsSpanish ? "Reserva confirmada" : "Booking confirmed";
    public static string Confirm_AlreadyHadAll => IsSpanish ? "Ya tenías reservados todos esos viajes." : "You had already booked all of those trips.";
    public static string Confirm_CouldNotBook => IsSpanish ? "No se pudo reservar.\n\n{0}" : "Couldn't book.\n\n{0}";
    public static string Confirm_SomeNotBooked => IsSpanish ? "Algunos viajes no se pudieron reservar. Revisá tu panel." : "Some trips couldn't be booked. Check your dashboard.";
    public static string Confirm_ServerRejected => IsSpanish ? "El servidor no aceptó las reservas." : "The server didn't accept the bookings.";
    public static string EditTrip_ReservedOne => IsSpanish ? "👥 1 pasajero reservado este día" : "👥 1 passenger booked this day";
    public static string EditTrip_ReservedMany => IsSpanish ? "👥 {0} pasajeros reservados este día" : "👥 {0} passengers booked this day";
    public static string EditTrip_ScopeOnlyThis => IsSpanish ? "Solo este viaje (Día seleccionado)" : "Only this trip (selected day)";
    public static string EditTrip_ScopeWeek => IsSpanish ? "Aplicar por 1 Semana (7 días)" : "Apply for 1 week (7 days)";
    public static string EditTrip_ScopeMonth => IsSpanish ? "Aplicar por 1 Mes (30 días)" : "Apply for 1 month (30 days)";
    public static string EditTrip_ScopeSeries => IsSpanish ? "Toda la serie (Infinito)" : "Entire series (no end)";
    public static string EditTrip_ScopeCustom => IsSpanish ? "Rango Personalizado..." : "Custom range...";
    public static string EditTrip_LoadError => IsSpanish ? "No se pudo cargar el viaje. Revisá tu conexión e intentá de nuevo." : "Couldn't load the trip. Check your connection and try again.";
    public static string EditTrip_UnitNew => IsSpanish ? "Unidad {0} · nueva" : "Unit {0} · new";
    public static string EditTrip_Unit => IsSpanish ? "Unidad {0}" : "Unit {0}";
    public static string EditTrip_SummaryOneUnit => IsSpanish ? "Este horario sale con 1 unidad." : "This time slot runs with 1 unit.";
    public static string EditTrip_SummaryManyUnits => IsSpanish ? "Este horario sale con {0} unidades a la misma hora." : "This time slot runs with {0} units at the same time.";
    public static string EditTrip_NeedOneUnitDelete => IsSpanish ? "El horario necesita al menos una unidad. Si querés cancelar el horario completo, usá el botón \"Eliminar horario\"." : "The time slot needs at least one unit. To cancel the whole time slot, use the \"Delete time slot\" button.";
    public static string EditTrip_UnitHasPassengersTitle => IsSpanish ? "Esta unidad tiene pasajeros" : "This unit has passengers";
    public static string EditTrip_UnitHasPassengersMsg => IsSpanish ? "{0}. Al guardar, sus reservas se pasarán a otra unidad de este mismo horario, para que nadie pierda su lugar.\n\n¿Quitar esta unidad?" : "{0}. When you save, their bookings will move to another unit in this same time slot so nobody loses their seat.\n\nRemove this unit?";
    public static string EditTrip_YesRemove => IsSpanish ? "Sí, quitar" : "Yes, remove";
    public static string EditTrip_RestrictedTitle => IsSpanish ? "Acceso Restringido" : "Restricted Access";
    public static string EditTrip_RestrictedMsg => IsSpanish ? "Solo el Chofer Jefe puede modificar la programación." : "Only the Fleet Owner can modify the schedule.";
    public static string EditTrip_NeedOneUnit => IsSpanish ? "El horario necesita al menos una unidad." : "The time slot needs at least one unit.";
    public static string EditTrip_UnitIncomplete => IsSpanish ? "En \"{0}\" falta elegir el chofer o el microbús." : "In \"{0}\" you still need to choose the driver or the bus.";
    public static string EditTrip_DriverDuplicated => IsSpanish ? "{0} está en dos unidades a la vez. Cada unidad necesita un chofer distinto." : "{0} is in two units at once. Each unit needs a different driver.";
    public static string EditTrip_BusDuplicated => IsSpanish ? "El microbús {0} está en dos unidades a la vez. Cada unidad necesita un microbús distinto." : "Bus {0} is in two units at once. Each unit needs a different bus.";
    public static string EditTrip_NotAllSaved => IsSpanish ? "No se guardó todo" : "Not everything was saved";
    public static string EditTrip_WaypointsError => IsSpanish ? "No se pudieron guardar los destinos: {0}" : "Couldn't save the destinations: {0}";
    public static string EditTrip_NewUnitsError => IsSpanish ? "No se pudieron crear las unidades nuevas: {0}" : "Couldn't create the new units: {0}";
    public static string EditTrip_MoveBookingsError => IsSpanish ? "No se pudieron pasar unas reservas: {0}" : "Some bookings couldn't be moved: {0}";
    public static string EditTrip_UnitRemovedReason => IsSpanish ? "Unidad retirada del horario" : "Unit removed from the time slot";
    public static string EditTrip_RemoveUnitError => IsSpanish ? "No se pudo quitar la unidad: {0}" : "Couldn't remove the unit: {0}";
    public static string EditTrip_DaysNotDeleted => IsSpanish ? "{0} día(s) de una unidad quitada no se borraron porque sus reservas no se pudieron pasar a otra unidad." : "{0} day(s) of a removed unit weren't deleted because their bookings couldn't be moved to another unit.";
    public static string EditTrip_TripDataError => IsSpanish ? "Las unidades se guardaron, pero no se pudieron actualizar los datos generales del viaje (tipo, horario real o destino)." : "The units were saved, but the trip's general data (type, real schedule or destination) couldn't be updated.";
    public static string EditTrip_SavedOneDay => IsSpanish ? "Cambios guardados para 1 día." : "Changes saved for 1 day.";
    public static string EditTrip_SavedManyDays => IsSpanish ? "Cambios guardados para {0} días." : "Changes saved for {0} days.";
    public static string EditTrip_BookingsMoved => IsSpanish ? "Las reservas de la unidad quitada se pasaron a otra unidad del mismo horario." : "Bookings from the removed unit were moved to another unit in the same time slot.";
    public static string EditTrip_SaveErrorDetail => IsSpanish ? "Ocurrió un error al guardar. Algunos cambios pudieron quedar a medias; revisá el calendario antes de volver a intentar.\n\nDetalle: {0}" : "An error occurred while saving. Some changes may be incomplete; check the calendar before trying again.\n\nDetail: {0}";
    public static string EditTrip_WhichDaysCancel => IsSpanish ? "¿Qué días querés cancelar? (se cancelan TODAS las unidades de este horario)" : "Which days do you want to cancel? (ALL units in this time slot will be cancelled)";
    public static string EditTrip_CheckSlotError => IsSpanish ? "No se pudo revisar el horario. Revisá tu conexión." : "Couldn't check the time slot. Check your connection.";
    public static string EditTrip_NoTripsInRange => IsSpanish ? "No hay viajes de este horario en esas fechas." : "There are no trips for this time slot on those dates.";
    public static string EditTrip_DefaultCancelReason => IsSpanish ? "Horario eliminado por la flota" : "Time slot removed by the fleet";
    public static string EditTrip_RangeFromOnward => IsSpanish ? "desde el {0} en adelante (los días anteriores se conservan como historial)" : "from {0} onward (earlier days are kept as history)";
    public static string EditTrip_RangeSingle => IsSpanish ? "del {0}" : "on {0}";
    public static string EditTrip_RangeBetween => IsSpanish ? "del {0} al {1}" : "from {0} to {1}";
    public static string EditTrip_WillCancel => IsSpanish ? "Se cancelará este horario {0}" : "This time slot will be cancelled {0}";
    public static string EditTrip_OneTrip => IsSpanish ? " (1 viaje)." : " (1 trip).";
    public static string EditTrip_ManyTrips => IsSpanish ? " ({0} viajes en total, contando todas las unidades)." : " ({0} trips in total, counting all units).";
    public static string EditTrip_OneBookingCancelled => IsSpanish ? "\n\n👥 1 reserva de pasajero se cancelará." : "\n\n👥 1 passenger booking will be cancelled.";
    public static string EditTrip_ManyBookingsCancelled => IsSpanish ? "\n\n👥 {0} reservas de pasajeros se cancelarán." : "\n\n👥 {0} passenger bookings will be cancelled.";
    public static string EditTrip_ReasonTheyWillSee => IsSpanish ? "\nMotivo que verán: \"{0}\"" : "\nReason they'll see: \"{0}\"";
    public static string EditTrip_RemoveOnlyOneHint => IsSpanish ? "\n\nPara quitar solo una unidad, usá \"✕ Quitar\" en esa unidad y luego \"Guardar Cambios\"." : "\n\nTo remove just one unit, use \"✕ Remove\" on that unit and then \"Save Changes\".";
    public static string EditTrip_CancelTripsTitle => IsSpanish ? "¿Cancelar viajes?" : "Cancel trips?";
    public static string EditTrip_YesCancel => IsSpanish ? "Sí, cancelar" : "Yes, cancel";
    public static string EditTrip_CancelledOnePax => IsSpanish ? "Viajes cancelados. El pasajero verá el aviso con el motivo la próxima vez que abra la app." : "Trips cancelled. The passenger will see the notice with the reason the next time they open the app.";
    public static string EditTrip_CancelledManyPax => IsSpanish ? "Viajes cancelados. Los {0} pasajeros verán el aviso con el motivo la próxima vez que abran la app." : "Trips cancelled. The {0} passengers will see the notice with the reason the next time they open the app.";
    public static string EditTrip_Cancelled => IsSpanish ? "Viajes cancelados." : "Trips cancelled.";
    public static string EditTrip_NothingCancelled => IsSpanish ? "No se canceló nada.\n\n{0}" : "Nothing was cancelled.\n\n{0}";
    public static string Fmt_DayShortMonth => IsSpanish ? "dd MMM" : "MMM dd";
    public static string FleetSched_BusGeneric => IsSpanish ? "🚐 Microbús" : "🚐 Bus";
    public static string FleetSched_With => IsSpanish ? "Con {0}" : "With {0}";
    public static string FleetSched_ManyBuses => IsSpanish ? "👥 {0} micros · tocá para elegir" : "👥 {0} buses · tap to choose";
    public static string FleetSched_TodayTag => IsSpanish ? "hoy" : "today";
    public static string FleetSched_DefaultTitle => IsSpanish ? "Horarios" : "Schedules";
    public static string FleetSched_PickYourTimes => IsSpanish ? "Elegí tus horarios" : "Pick your times";
    public static string FleetSched_NoTripsDay => IsSpanish ? "No hay viajes este día." : "No trips this day.";
    public static string FleetSched_ViewByDay => IsSpanish ? "📋 Ver por día" : "📋 View by day";
    public static string FleetSched_ViewAsTimetable => IsSpanish ? "📅 Ver como horario" : "📅 View as timetable";
    public static string FleetSched_TitleOf => IsSpanish ? "Horarios de {0}" : "{0}'s schedules";
    public static string FleetSched_DriverDefault => IsSpanish ? "Chofer" : "Driver";
    public static string FleetSched_NoSchedulesYet => IsSpanish ? "Esta flota todavía no tiene horarios cargados para las próximas semanas." : "This fleet doesn't have schedules for the coming weeks yet.";
    public static string FleetSched_LoadError => IsSpanish ? "No se pudieron cargar los horarios. Revisá tu conexión." : "Couldn't load the schedules. Check your connection.";
    public static string FleetSched_Arrives => IsSpanish ? "llega {0}" : "arrives {0}";
    public static string FleetSched_WeekRange => IsSpanish ? "Semana del {0} al {1}" : "Week of {0} to {1}";
    public static string FleetSched_ArriveAtU => IsSpanish ? "Llegar a la U" : "Arrive at campus";
    public static string FleetSched_LeaveU => IsSpanish ? "Salir de la U" : "Leave campus";
    public static string FleetSched_WhoWith => IsSpanish ? "¿Con quién querés ir?" : "Who do you want to ride with?";
    public static string Fmt_DayAbbrevNum => IsSpanish ? "ddd d" : "ddd d";
    public static string Fmt_DayNumMonth => IsSpanish ? "d MMM" : "MMM d";
    public static string FleetSched_TodayPrefix => IsSpanish ? "Hoy · {0}" : "Today · {0}";
    public static string FleetSched_PastSuffix => IsSpanish ? "{0} (ya pasó)" : "{0} (past)";
    public static string FleetSched_ArrivalAtU => IsSpanish ? "Llegada a la U" : "Arrival at campus";
    public static string FleetSched_DepartureFromU => IsSpanish ? "Salida de la U" : "Departure from campus";
    public static string FleetSched_AlreadyBookedTitle => IsSpanish ? "Ya lo tenés reservado" : "Already booked";
    public static string FleetSched_AlreadyBookedMsg => IsSpanish ? "Este viaje ya está en tu panel. Si querés anularlo, hacelo desde \"Mi Panel\"." : "This trip is already on your dashboard. To cancel it, do it from \"My Dashboard\".";
    public static string FleetSched_TripAlreadyLeft => IsSpanish ? "Ese viaje ya salió. Los días pasados son solo para ver." : "That trip already left. Past days are view-only.";
    public static string FleetSched_ContinueOne => IsSpanish ? "Continuar con 1 viaje" : "Continue with 1 trip";
    public static string FleetSched_ContinueMany => IsSpanish ? "Continuar con {0} viajes" : "Continue with {0} trips";
    public static string FleetSched_TapAtLeastOne => IsSpanish ? "Tocá al menos un horario para reservar." : "Tap at least one time to book.";
    public static string Cancel_ReasonNoClasses => IsSpanish ? "📚 No hay clases" : "📚 No classes";
    public static string Cancel_ReasonRain => IsSpanish ? "🌧️ Lluvia o fenómeno natural" : "🌧️ Rain or natural event";
    public static string Cancel_ReasonDriverSick => IsSpanish ? "🤒 El chofer está enfermo" : "🤒 The driver is sick";
    public static string Cancel_ReasonMechanical => IsSpanish ? "🔧 Falla mecánica del microbús" : "🔧 Bus mechanical failure";
    public static string Cancel_ReasonFewPassengers => IsSpanish ? "👥 Muy pocos pasajeros" : "👥 Too few passengers";
    public static string Cancel_ReasonOther => IsSpanish ? "✏️ Otro motivo (escribir)" : "✏️ Other reason (write it)";
    public static string Cancel_AskReasonOne => IsSpanish ? "1 pasajero tiene reserva. ¿Por qué se cancela? (lo va a ver)" : "1 passenger has a booking. Why is it being cancelled? (they'll see it)";
    public static string Cancel_AskReasonMany => IsSpanish ? "{0} reservas de pasajeros. ¿Por qué se cancela? (lo van a ver)" : "{0} passenger bookings. Why is it being cancelled? (they'll see it)";
    public static string Cancel_ReasonPromptTitle => IsSpanish ? "Motivo de la cancelación" : "Cancellation reason";
    public static string Cancel_ReasonPromptMsg => IsSpanish ? "Escribilo en pocas palabras. Tus pasajeros lo van a ver." : "Write it in a few words. Your passengers will see it.";
    public static string Cancel_ReasonPromptPlaceholder => IsSpanish ? "Ej.: paro de transporte" : "E.g.: transport strike";
    public static string Cancel_SupabaseMissingFunction => IsSpanish ? "Falta configurar Supabase: corré el archivo supabase_cancelaciones.sql en el SQL Editor." : "Supabase isn't set up yet: run supabase_cancelaciones.sql in the SQL Editor.";
    public static string Trip_Generic => IsSpanish ? "Viaje" : "Trip";
    public static string Cancel_RangeDays => IsSpanish ? "del {0} al {1} ({2} días)" : "from {0} to {1} ({2} days)";
    public static string Fmt_DayAbbrevNumMonth => IsSpanish ? "ddd d MMM" : "ddd, MMM d";
    public static string Cancel_ReasonLine => IsSpanish ? "   Motivo: {0}" : "   Reason: {0}";
    public static string Cancel_MoreSlots => IsSpanish ? "…y {0} horario(s) más." : "…and {0} more time slot(s).";
    public static string Cancel_NoLongerActive => IsSpanish ? "Tu reserva ya no está activa para esos días. Si necesitás transporte, podés reservar otro horario." : "Your booking is no longer active for those days. If you need transport, you can book another time.";
    public static string Cancel_TitleOne => IsSpanish ? "🚫 Se canceló un viaje que tenías reservado" : "🚫 A trip you had booked was cancelled";
    public static string Cancel_TitleMany => IsSpanish ? "🚫 Se cancelaron {0} viajes que tenías reservados" : "🚫 {0} trips you had booked were cancelled";
    public static string Global_Diagnostic => IsSpanish ? "\n\nDIAGNÓSTICO: {0}" : "\n\nDIAGNOSTIC: {0}";
    public static string Vehicle_PhotoUploadError => IsSpanish ? "No se pudo subir la foto. Revisa tu conexión e intenta de nuevo." : "Couldn't upload the photo. Check your connection and try again.";
    public static string AdminFleet_CodeError => IsSpanish ? "ERR-CÓD" : "CODE-ERR";
    public static string AdminFleet_RegenerateTitle => IsSpanish ? "Regenerar Código" : "Regenerate Code";
    public static string AdminFleet_RegenerateConfirm => IsSpanish ? "¿Estás seguro de que deseas cambiar tu código de flota? El código anterior dejará de funcionar para nuevos empleados." : "Are you sure you want to change your fleet code? The old code will stop working for new employees.";
    public static string AdminFleet_YesChange => IsSpanish ? "Sí, cambiar" : "Yes, change";
    public static string AdminFleet_CodeUpdated => IsSpanish ? "Tu código de flota ha sido actualizado." : "Your fleet code has been updated.";
    public static string AdminFleet_RegenerateError => IsSpanish ? "No se pudo regenerar el código." : "Couldn't regenerate the code.";
    public static string AdminFleet_DeactivateBusConfirm => IsSpanish ? "¿Desactivar el microbús {0}? Ya no aparecerá disponible para programar viajes nuevos, pero su historial se conserva." : "Deactivate bus {0}? It won't be available for scheduling new trips, but its history is kept.";
    public static string AdminFleet_ReactivateBusConfirm => IsSpanish ? "¿Reactivar el microbús {0}?" : "Reactivate bus {0}?";
    public static string AdminFleet_BusUpdateError => IsSpanish ? "No se pudo actualizar el microbús." : "Couldn't update the bus.";
    public static string AdminFleet_RemoveEmployeeConfirm => IsSpanish ? "¿Sacar a {0} de tu flota? Perderá acceso inmediato a los viajes y datos de tu equipo." : "Remove {0} from your fleet? They'll immediately lose access to your team's trips and data.";
    public static string AdminFleet_YesRemoveEmployee => IsSpanish ? "Sí, sacarlo" : "Yes, remove";
    public static string AdminFleet_RemoveEmployeeError => IsSpanish ? "No se pudo sacar al empleado." : "Couldn't remove the employee.";
    public static string AdminFleet_DeleteBusConfirm => IsSpanish ? "¿Estás seguro de que deseas eliminar el microbús {0} de tu flota?" : "Are you sure you want to delete bus {0} from your fleet?";
    public static string AdminFleet_YesDelete => IsSpanish ? "Sí, eliminar" : "Yes, delete";
    public static string AdminFleet_DeleteBusError => IsSpanish ? "No se pudo eliminar el microbús. Es posible que tenga viajes asignados en el historial." : "Couldn't delete the bus. It may have trips assigned in its history.";
    public static string Auth_NewAccountComplete => IsSpanish ? "Cuenta nueva. Por favor completa tu registro." : "New account. Please complete your registration.";
    public static string Auth_PasswordsDontMatch => IsSpanish ? "Las contraseñas no coinciden." : "Passwords don't match.";
    public static string Auth_SelectRoleAndName => IsSpanish ? "Por favor selecciona un rol y escribe tu nombre." : "Please select a role and enter your name.";
    public static string Auth_Step1DataLost => IsSpanish ? "Los datos del paso 1 se perdieron. Por favor, dale Atrás y vuelve a ingresarlos." : "The step 1 data was lost. Please go Back and enter it again.";
    public static string Auth_CheckEmailTitle => IsSpanish ? "¡Revisa tu correo!" : "Check your email!";
    public static string Auth_CheckEmailMsg => IsSpanish ? "Tu cuenta fue creada. Por favor confirma tu correo electrónico antes de iniciar sesión." : "Your account was created. Please confirm your email before signing in.";
    public static string Auth_ProfilePhotoPicker => IsSpanish ? "Foto de perfil" : "Profile photo";
    public static string Shell_Guest => IsSpanish ? "Invitado" : "Guest";
    public static string Settings_NotRegistered => IsSpanish ? "No registrado" : "Not registered";
    public static string Settings_NoPhone => IsSpanish ? "Sin teléfono registrado" : "No phone on file";
    public static string Settings_CodeGenError => IsSpanish ? "Error generando código" : "Error generating code";
    public static string Settings_UnknownBoss => IsSpanish ? "Jefe Desconocido" : "Unknown boss";
    public static string Directory_SessionInactive => IsSpanish ? "Tu sesión no está activa.\nCerrá sesión desde el menú y volvé a entrar." : "Your session isn't active.\nLog out from the menu and sign in again.";
    public static string Directory_NoFleetsYet => IsSpanish ? "Todavía no hay flotas registradas." : "There are no registered fleets yet.";
    public static string Directory_LoadError => IsSpanish ? "No se pudieron cargar las flotas.\n{0}" : "Couldn't load the fleets.\n{0}";
    public static string Directory_NoMatch => IsSpanish ? "Ninguna flota coincide con tu búsqueda." : "No fleet matches your search.";
    public static string Track_WaitingSignal => IsSpanish ? "⏳ Esperando la señal de tu chofer…" : "⏳ Waiting for your driver's signal…";
    public static string Track_NotSharing => IsSpanish ? "📡 Tu chofer no está compartiendo su ubicación en este momento." : "📡 Your driver isn't sharing their location right now.";
    public static string Track_OnTheWay => IsSpanish ? "🟢 En camino · actualizado {0}" : "🟢 On the way · updated {0}";
    public static string Track_WeakSignal => IsSpanish ? "🟠 Señal débil · última vez {0}" : "🟠 Weak signal · last seen {0}";
    public static string Track_SecondsAgo => IsSpanish ? "hace {0} s" : "{0} s ago";
    public static string Track_MinutesAgo => IsSpanish ? "hace {0} min" : "{0} min ago";
    public static string PaxDetail_Pending => IsSpanish ? "Pendiente" : "Pending";
    public static string PaxDetail_BusOnTheWay => IsSpanish ? "Tu microbús está en camino" : "Your bus is on the way";
    public static string TripDetails_PointNotSetTitle => IsSpanish ? "Punto no definido" : "Point not set";
    public static string TripDetails_PointNotSetMsg => IsSpanish ? "El pasajero no ha definido este punto en el mapa." : "The passenger hasn't set this point on the map.";
    public static string Schedule_LoadScreenError => IsSpanish ? "Error al cargar la pantalla de Crear Viaje" : "Error loading the Create Trip screen";

    /// <summary>Rol guardado en la BD (pasajero, chofer_jefe...) → texto en el idioma de la app.</summary>
    public static string RoleLabel(string? rol)
    {
        string r = (rol ?? "").ToLowerInvariant();
        if (r.Contains("jefe") || r.Contains("owner")) return RegisterStep2RoleDriverOwner;
        if (r.Contains("empleado") || r.Contains("employee")) return RegisterStep2RoleDriverEmployee;
        if (r.Contains("independiente") || r.Contains("independent")) return RegisterStep2RoleDriverIndependent;
        if (r.Contains("pasajero") || r.Contains("passenger")) return RegisterStep2RolePassenger;
        return rol ?? "";
    }

    // ===== GPS DEL CHOFER CON LA PANTALLA APAGADA =====
    public static string LiveTrack_ServiceTitle => IsSpanish ? "PassGold · Viaje en curso" : "PassGold · Trip in progress";
    public static string LiveTrack_ServiceText => IsSpanish
        ? "Compartiendo tu ubicación con tus pasajeros hasta las {0}. Puedes apagar la pantalla."
        : "Sharing your location with your passengers until {0}. You can turn the screen off.";

    // ===== CAMBIO DE ROL =====
    public static string Role_SectionTitle => IsSpanish ? "Mi rol en PassGold" : "My role in PassGold";
    public static string Role_ChangeBtn => IsSpanish ? "🔄 Cambiar" : "🔄 Change";
    public static string Role_ChooseTitle => IsSpanish ? "¿A qué rol te quieres cambiar?" : "Which role do you want to switch to?";
    public static string Role_ConfirmTitle => IsSpanish ? "¿Cambiar a {0}?" : "Switch to {0}?";
    public static string Role_ConfirmYes => IsSpanish ? "Sí, cambiar" : "Yes, switch";
    public static string Role_Continue => IsSpanish ? "Continuar" : "Continue";
    public static string Role_CodeTitle => IsSpanish ? "Código de la flota" : "Fleet code";
    public static string Role_CodeMsg => IsSpanish ? "Escribe el código de 6 letras que te dio el jefe de la flota." : "Enter the 6-letter code the fleet owner gave you.";
    public static string Role_NoUndo => IsSpanish ? "\n\nEsto no se puede deshacer." : "\n\nThis can't be undone.";
    public static string Role_FromPassenger => IsSpanish
        ? "Tu nombre, correo, foto y teléfono se mantienen. Se borrarán tus reservas (los asientos quedarán libres), tus lugares guardados y tus avisos."
        : "Your name, email, photo and phone stay the same. Your bookings (the seats will be freed), saved places and notices will be deleted.";
    public static string Role_IndepToBoss => IsSpanish
        ? "Se mantienen todos tus micros, horarios y pasajeros. Además tendrás un código para invitar choferes empleados a tu flota."
        : "All your buses, schedules and passengers stay. You'll also get a code to invite employee drivers to your fleet.";
    public static string Role_IndepToEmployee => IsSpanish
        ? "Tus micros, horarios y rutas pasarán a la flota de tu nuevo jefe. Seguirás manejando tus viajes y tus pasajeros conservan su asiento.\n\nYa no podrás crear ni editar viajes, a menos que tu jefe te dé permiso."
        : "Your buses, schedules and routes will move to your new boss's fleet. You'll keep driving your trips and your passengers keep their seats.\n\nYou won't be able to create or edit trips unless your boss allows it.";
    public static string Role_BossToIndep => IsSpanish
        ? "Mantienes tus micros y horarios. Tus choferes empleados quedarán sin flota y los viajes que tenían asignados pasarán a ti."
        : "You keep your buses and schedules. Your employee drivers will be left without a fleet and the trips assigned to them will go to you.";
    public static string Role_BossToEmployee => IsSpanish
        ? "Tu flota se cerrará: se borrarán tus horarios y micros, los pasajeros con viajes futuros recibirán un aviso de cancelación y tus choferes quedarán sin flota."
        : "Your fleet will close: your schedules and buses will be deleted, passengers with upcoming trips will get a cancellation notice and your drivers will be left without a fleet.";
    public static string Role_CloseFleetToPassenger => IsSpanish
        ? "Tu flota se cerrará: se borrarán tus horarios y micros y los pasajeros con viajes futuros recibirán un aviso de cancelación. Tu nombre, correo, foto y teléfono se mantienen."
        : "Your fleet will close: your schedules and buses will be deleted and passengers with upcoming trips will get a cancellation notice. Your name, email, photo and phone stay the same.";
    public static string Role_EmployeeToOwner => IsSpanish
        ? "Saldrás de la flota de tu jefe y empezarás la tuya. Los viajes que tenías asignados vuelven a tu jefe."
        : "You'll leave your boss's fleet and start your own. The trips assigned to you go back to your boss.";
    public static string Role_EmployeeToPassenger => IsSpanish
        ? "Saldrás de la flota de tu jefe. Los viajes que tenías asignados vuelven a él. Tu nombre, correo, foto y teléfono se mantienen."
        : "You'll leave your boss's fleet. The trips assigned to you go back to your boss. Your name, email, photo and phone stay the same.";
    public static string Role_DoneTitle => IsSpanish ? "¡Listo!" : "Done!";
    public static string Role_DoneMsg => IsSpanish ? "Ahora eres {0}." : "You are now {0}.";
    public static string Role_DoneJoined => IsSpanish ? " Te uniste a la flota de {0}." : " You joined {0}'s fleet.";
    public static string Role_ErrInvalidCode => IsSpanish ? "Ese código no es de ninguna flota. Revisa que esté bien escrito (6 letras) y que sea el de un chofer jefe." : "That code doesn't belong to any fleet. Check it's typed correctly (6 letters) and that it's a fleet owner's code.";
    public static string Role_ErrSame => IsSpanish ? "Ya tienes ese rol." : "You already have that role.";
    public static string Role_ErrGeneric => IsSpanish ? "No se pudo cambiar el rol. Revisa tu internet e inténtalo de nuevo." : "The role couldn't be changed. Check your internet and try again.";

    // ===== SUB-JEFE (permiso para crear y editar viajes) =====
    public static string SubBoss_SwitchTitle => IsSpanish ? "Puede crear y editar viajes" : "Can create and edit trips";
    public static string SubBoss_SwitchDesc => IsSpanish ? "Lo convierte en sub-jefe de tu flota." : "Makes them a sub-boss of your fleet.";
    public static string SubBoss_Badge => IsSpanish ? "⭐ Sub-jefe" : "⭐ Sub-boss";
    public static string SubBoss_ConfirmTitle => IsSpanish ? "¿Dar permiso de sub-jefe?" : "Give sub-boss permission?";
    public static string SubBoss_ConfirmMsg => IsSpanish
        ? "{0} podrá crear, editar y cancelar los viajes de tu flota y asignarlos a cualquier chofer. Puedes quitarle el permiso cuando quieras."
        : "{0} will be able to create, edit and cancel your fleet's trips and assign them to any driver. You can remove the permission at any time.";
    public static string SubBoss_ConfirmYes => IsSpanish ? "Sí, dar permiso" : "Yes, allow";
    public static string SubBoss_Error => IsSpanish ? "No se pudo guardar el permiso. Revisa tu internet e inténtalo de nuevo." : "The permission couldn't be saved. Check your internet and try again.";

    // ===== PRIVACIDAD Y ELIMINAR CUENTA =====
    public static string Settings_LegalTitle => IsSpanish ? "Privacidad y cuenta" : "Privacy & account";
    public static string Settings_PrivacyBtn => IsSpanish ? "🔒 Política de privacidad" : "🔒 Privacy policy";
    public static string Settings_TermsBtn => IsSpanish ? "📄 Términos de uso" : "📄 Terms of use";
    public static string Settings_DeleteAccountBtn => IsSpanish ? "Eliminar mi cuenta" : "Delete my account";
    public static string DeleteAcc_ConfirmTitle => IsSpanish ? "¿Eliminar tu cuenta?" : "Delete your account?";
    public static string DeleteAcc_MsgPassenger => IsSpanish
        ? "Se borrarán para siempre tu perfil, tus reservas (los asientos quedarán libres), tus lugares guardados y tus fotos.\n\nEsto no se puede deshacer."
        : "Your profile, your bookings (the seats will be freed), your saved places and your photos will be permanently deleted.\n\nThis can't be undone.";
    public static string DeleteAcc_MsgEmployee => IsSpanish
        ? "Se borrarán para siempre tu perfil y tus fotos. Los viajes que tenías asignados no se borran: pasan al dueño de la flota para que los asigne a otro chofer.\n\nEsto no se puede deshacer."
        : "Your profile and photos will be permanently deleted. The trips assigned to you are not deleted: they go back to the fleet owner so another driver can take them.\n\nThis can't be undone.";
    public static string DeleteAcc_MsgOwner => IsSpanish
        ? "Se borrarán para siempre tu perfil, tu flota, tus horarios, tus vehículos y sus fotos. Los pasajeros con viajes futuros recibirán un aviso de cancelación y tus choferes empleados quedarán sin flota (sus cuentas no se borran).\n\nEsto no se puede deshacer."
        : "Your profile, your fleet, your schedules, your vehicles and their photos will be permanently deleted. Passengers with upcoming trips will get a cancellation notice, and your employee drivers will be left without a fleet (their accounts are not deleted).\n\nThis can't be undone.";
    public static string DeleteAcc_Continue => IsSpanish ? "Continuar" : "Continue";
    public static string DeleteAcc_Cancel => IsSpanish ? "Cancelar" : "Cancel";
    public static string DeleteAcc_TypeTitle => IsSpanish ? "Confirmación final" : "Final confirmation";
    public static string DeleteAcc_TypeWord => IsSpanish ? "ELIMINAR" : "DELETE";
    public static string DeleteAcc_TypeMsg => IsSpanish ? "Escribe ELIMINAR para borrar tu cuenta." : "Type DELETE to erase your account.";
    public static string DeleteAcc_TypeBtn => IsSpanish ? "Eliminar cuenta" : "Delete account";
    public static string DeleteAcc_WrongWord => IsSpanish ? "No escribiste la palabra correcta. Tu cuenta no se eliminó." : "That's not the right word. Your account was not deleted.";
    public static string DeleteAcc_DoneTitle => IsSpanish ? "Cuenta eliminada" : "Account deleted";
    public static string DeleteAcc_DoneMsg => IsSpanish ? "Tu cuenta y tus datos se eliminaron. Gracias por usar PassGold." : "Your account and data have been deleted. Thank you for using PassGold.";
    public static string DeleteAcc_Error => IsSpanish
        ? "No se pudo eliminar la cuenta. Revisa tu internet e inténtalo de nuevo. Si el problema sigue, escríbenos a {0}."
        : "The account couldn't be deleted. Check your internet and try again. If it keeps failing, email us at {0}.";
    public static string Legal_OpenError => IsSpanish ? "No se pudo abrir la página. Visítala en {0}" : "The page couldn't be opened. Visit it at {0}";

    // ===== END TEXTOS NUEVOS =====

}