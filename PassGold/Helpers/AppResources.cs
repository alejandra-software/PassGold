using System.Globalization;

namespace PassGold.Helpers;

public static class AppResources
{
    private static bool IsSpanish
    {
        get
        {
            try { return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "es"; }
            catch { return true; }
        }
    }

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

    // --- ALERTS (ADD/EDIT VEHICLE) ---
    public static string AlertEmptyFields => IsSpanish ? "Por favor llena todos los campos." : "Please fill in all fields.";
    public static string AlertInvalidCapacity => IsSpanish ? "La capacidad debe ser un número válido." : "Capacity must be a valid number.";
    public static string AlertVehicleSaved => IsSpanish ? "El microbús ha sido guardado correctamente." : "The minibus has been saved successfully.";
    public static string AlertVehicleSaveError => IsSpanish ? "No se pudo guardar el vehículo." : "Could not save the vehicle.";
    public static string AlertVehicleUpdated => IsSpanish ? "El microbús ha sido actualizado." : "The minibus has been updated.";
    public static string AlertVehicleUpdateError => IsSpanish ? "No se pudo actualizar el vehículo." : "Could not update the vehicle.";
    public static string AlertVehicleDeleted => IsSpanish ? "El microbús ha sido eliminado." : "The minibus has been deleted.";

    // --- AGREGAR / EDITAR VEHÍCULO ---
    //  Faltaban por completo — por eso se veían las llaves literales entre corchetes
    // (ej. "[EditVehicle_Title]") en vez del texto traducido.
    public static string AddVehicleTitle => IsSpanish ? "Añadir Microbús" : "Add Minibus";
    public static string AddVehicleHeader => IsSpanish ? "Nueva Unidad" : "New Unit";
    public static string AddVehicleDesc => IsSpanish ? "Registra los datos del microbús para tu flota." : "Register the minibus details for your fleet.";
    public static string AddVehiclePlateLabel => IsSpanish ? "Placa" : "License Plate";
    public static string AddVehiclePlatePlaceholder => IsSpanish ? "Ej. N1234" : "E.g. N1234";
    public static string AddVehicleCapacityLabel => IsSpanish ? "Capacidad de Pasajeros" : "Passenger Capacity";
    public static string AddVehicleCapacityPlaceholder => IsSpanish ? "Ej. 35" : "E.g. 35";
    public static string AddVehicleSaveBtn => IsSpanish ? "Guardar Microbús" : "Save Minibus";

    public static string EditVehicle_Title => IsSpanish ? "Editar Microbús" : "Edit Minibus";
    public static string EditVehicle_Header => IsSpanish ? "Editar Unidad" : "Edit Unit";
    public static string EditVehicle_Desc => IsSpanish ? "Actualiza los datos de este microbús." : "Update this minibus's details.";
    public static string EditVehicle_PhotoLabel => IsSpanish ? "Foto de la Unidad" : "Unit Photo";
    public static string EditVehicle_SaveBtn => IsSpanish ? "Guardar Cambios" : "Save Changes";

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

    // --- FLEET PROFILE (PERFIL SOCIAL DE FLOTA) ---
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

    // 🔥 LOGÍSTICA DE FLOTA
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

    // 🆕 Ficha de detalle del pasajero — foto grande + botón de llamar.
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
    public static string PassengerDash_HomeLabel => IsSpanish ? "🏠 Mi Casa" : "🏠 My Home";
    public static string PassengerDash_MixedDirectionError => IsSpanish ? "Seleccionaste viajes de Ida y de Regreso juntos — hacelo por separado, uno primero y otro después." : "You selected both Inbound and Outbound trips together — please book them separately.";
    public static string PassengerDash_NeedHomeFirst => IsSpanish ? "Antes de tu primera reserva, marcá en el mapa dónde está tu casa. Solo lo vas a hacer una vez — después de guardarla, volvé a presionar Reservar." : "Before your first booking, mark your home on the map. You'll only do this once — after saving it, tap Book again.";
    public static string SetHome_Title => IsSpanish ? "Guardar Mi Casa" : "Save My Home";
    public static string SetHome_Instructions => IsSpanish ? "Tocá el mapa en el punto exacto de tu casa (o cerca, si vive en un lugar sin acceso directo)." : "Tap the map at the exact spot of your home (or nearby, if it's somewhere without direct access).";
    public static string SetHome_SaveBtn => IsSpanish ? "Guardar Mi Casa" : "Save My Home";
    public static string SetHome_SavedSuccess => IsSpanish ? "¡Listo! Tu casa quedó guardada. Ahora presioná Reservar de nuevo." : "Done! Your home is saved. Now tap Book again.";
    public static string SetHome_NoPinError => IsSpanish ? "Tocá el mapa primero para marcar tu casa." : "Tap the map first to mark your home.";
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

    // --- SETTINGS: EDICIÓN DE PERFIL Y CLOUDINARY ---
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

    // --- CONFIRMAR RESERVA (pantalla única) ---
    public static string ConfirmarReserva_Title => IsSpanish ? "Confirmar Reserva" : "Confirm Booking";
    public static string ConfirmarReserva_TocandoCasaHint => IsSpanish ? "🟢 Tocá el mapa para marcar TU CASA" : "🟢 Tap the map to mark YOUR HOME";
    public static string ConfirmarReserva_TocandoAlternoHint => IsSpanish ? "🔵 Tocá el mapa para el punto alterno" : "🔵 Tap the map for the alternate point";
    public static string ConfirmarReserva_CasaFijaDesc => IsSpanish ? "Fijo: siempre es el mismo punto en todos tus viajes." : "Fixed: it's always the same point on every trip.";
    public static string ConfirmarReserva_TocarCasaBtn => IsSpanish ? "📍 Tocar el mapa para marcar/mover mi casa" : "📍 Tap the map to set/move my home";
    public static string ConfirmarReserva_ParadasOficialesLabel => IsSpanish ? "Paradas oficiales del chofer:" : "Driver's official stops:";
    public static string ConfirmarReserva_MisUbicacionesLabel => IsSpanish ? "Tus ubicaciones guardadas:" : "Your saved locations:";
    public static string ConfirmarReserva_PinNuevoBtn => IsSpanish ? "📍 O tocar el mapa para un pin nuevo" : "📍 Or tap the map for a new pin";
    public static string ConfirmarReserva_GuardarNuevoLabel => IsSpanish ? "Guardar este lugar con un nombre para la próxima vez" : "Save this place with a name for next time";
    public static string ConfirmarReserva_AliasPlaceholder => IsSpanish ? "Ej. Casa de mi tía, Trabajo, Edificio I..." : "E.g. Aunt's house, Work, Building I...";
    public static string ConfirmarReserva_ElegidoLabel => IsSpanish ? "Elegido:" : "Chosen:";
    public static string ConfirmarReserva_ParaCuandoLabel => IsSpanish ? "¿Para cuándo?" : "For when?";
    public static string ConfirmarReserva_AlcancePickerTitle => IsSpanish ? "Elegí el alcance" : "Choose the scope";
    public static string ConfirmarReserva_ConfirmarBtn => IsSpanish ? "✅ Confirmar Reserva" : "✅ Confirm Booking";
    public static string ConfirmarReserva_Ubicando => IsSpanish ? "Ubicando..." : "Locating...";
    public static string ConfirmarReserva_PinSinDireccion => IsSpanish ? "📍 Punto marcado en el mapa" : "📍 Point marked on the map";
    public static string ConfirmarReserva_TabRecogida => IsSpanish ? "🟢 Recogida" : "🟢 Pickup";
    public static string ConfirmarReserva_TabBajada => IsSpanish ? "🔵 Bajada" : "🔵 Drop-off";
}