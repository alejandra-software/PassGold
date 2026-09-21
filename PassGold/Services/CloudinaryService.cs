using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SkiaSharp;

namespace PassGold.Services
{
    // PLAN B: después de agotar todas las variantes razonables de subida directa
    // desde Android a Cloudinary (cloud name, orden de campos, buffer en memoria,
    // SocketsHttpHandler, boundary sin comillas, forzar HTTP/1.1) y confirmar con
    // curl desde una PC que la cuenta/preset de Cloudinary funcionan perfecto, el
    // problema quedó acotado a algo irreductible del stack de red de Android en
    // este dispositivo/red específicos.
    //
    // Ahora la app ya NO le habla directo a Cloudinary. Le manda la imagen (en
    // base64, dentro de un JSON simple — nada de multipart desde el celular) a una
    // Edge Function de Supabase ("upload-to-cloudinary"), y es esa función,
    // corriendo en el servidor, la que arma el multipart y se lo manda a
    // Cloudinary. Esto saca el problema de la ecuación por completo.
    public class CloudinaryService
    {
        // Mismos valores que usa SupabaseService.cs — la Edge Function vive en el
        // mismo proyecto de Supabase.
        private const string SupabaseUrl = "";
        private const string SupabaseAnonKey = "api de cloudinary";
        // 🔧 Ajustado al nombre real que Supabase le asignó a la función al crearla
        // ("bright-responder" — el nombre en sí no importa para nada, la lógica es
        // la misma; si en algún momento se renombra en Supabase, actualizar acá).
        private static readonly string FunctionUrl = $"{SupabaseUrl}/functions/v1/bright-responder";

        private static CloudinaryService? _instance;
        public static CloudinaryService Instance => _instance ??= new CloudinaryService();

        private readonly HttpClient _httpClient;

        private CloudinaryService()
        {
            _httpClient = new HttpClient();
            // 🔧 Antes eran 30s, pero eso era corto para fotos sin comprimir de
            // varios MB en una red lenta. Ahora que comprimimos antes de subir
            // (ver ComprimirImagen) el payload es chico (normalmente <500KB),
            // pero dejamos algo de margen extra por si la conexión está mala.
            _httpClient.Timeout = TimeSpan.FromSeconds(45);
        }

        public async Task<(string? Url, string Error)> SubirImagenAsync(string rutaLocalArchivo, string carpeta)
        {
            if (string.IsNullOrWhiteSpace(rutaLocalArchivo) || !File.Exists(rutaLocalArchivo))
            {
                string err = "Ruta de archivo inválida o no existe.";
                Debug.WriteLine($"❌ Cloudinary: {err}");
                return (null, err);
            }

            try
            {
                using var stream = File.OpenRead(rutaLocalArchivo);
                return await SubirImagenAsync(stream, Path.GetFileName(rutaLocalArchivo), carpeta);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Cloudinary: error leyendo archivo local: {ex.Message}");
                return (null, ex.Message);
            }
        }

        public async Task<(string? Url, string Error)> SubirImagenAsync(Stream imagenStream, string nombreArchivo, string carpeta)
        {
            try
            {
                //  FIX: antes se mandaba la foto tal cual salía de la cámara/galería
                // (a veces 3-12 MB), convertida a base64 (+33% de peso) dentro de un
                // JSON. En wifi de universidad o datos móviles eso fácilmente superaba
                // el timeout o fallaba a medio camino — la causa más probable de que
                // "a veces no suba la foto". Ahora se redimensiona/comprime ANTES de
                // codificar, dejando normalmente <500KB sin pérdida visible en la app
                // (los avatares y fotos de micro nunca se ven a resolución completa).
                byte[] imagenBytes;
                long pesoOriginal;
                using (var bufferOriginal = new MemoryStream())
                {
                    await imagenStream.CopyToAsync(bufferOriginal);
                    pesoOriginal = bufferOriginal.Length;
                    bufferOriginal.Position = 0;
                    imagenBytes = await Task.Run(() => ComprimirImagen(bufferOriginal));
                }

                string base64 = Convert.ToBase64String(imagenBytes);

                Debug.WriteLine($"📦 Subiendo vía Edge Function — {pesoOriginal} bytes -> {imagenBytes.Length} bytes comprimidos, carpeta='{carpeta}'");

                var payload = new JObject
                {
                    ["imageBase64"] = base64,
                    ["fileName"] = nombreArchivo,
                    ["folder"] = carpeta
                };

                using var request = new HttpRequestMessage(HttpMethod.Post, FunctionUrl);
                request.Headers.Add("apikey", SupabaseAnonKey);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", SupabaseAnonKey);
                request.Content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"❌ Edge Function error ({(int)response.StatusCode}): {responseBody}");
                    return (null, $"HTTP {(int)response.StatusCode}: {responseBody}");
                }

                var parsed = JObject.Parse(responseBody);
                var url = parsed["url"]?.ToString();
                var errorMsg = parsed["error"]?.ToString();

                if (string.IsNullOrEmpty(url))
                {
                    Debug.WriteLine($"❌ Edge Function respondió sin URL: {responseBody}");
                    return (null, errorMsg ?? "Respuesta sin URL de la función.");
                }

                Debug.WriteLine($"✅ Imagen subida -> {url}");
                return (url, "");
            }
            catch (TaskCanceledException)
            {
                string err = "Tiempo de espera agotado (revisa conexión a internet).";
                Debug.WriteLine($"❌ Cloudinary: {err}");
                return (null, err);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Cloudinary: error subiendo imagen: {ex.Message}");
                return (null, ex.Message);
            }
        }

        //  Redimensiona al lado más largo = maxLado (sin agrandar fotos que ya
        // sean chicas) y re-codifica como JPEG con la calidad indicada. Corre
        // sincrónico a propósito — quien la llama la envuelve en Task.Run para no
        // trabar el hilo de UI mientras decodifica/comprime.
        private static byte[] ComprimirImagen(Stream original, int maxLado = 1280, int calidadJpeg = 82)
        {
            using var bitmapOriginal = SKBitmap.Decode(original);
            if (bitmapOriginal == null)
                throw new InvalidOperationException("No se pudo leer la imagen (formato no soportado o archivo dañado).");

            int ancho = bitmapOriginal.Width;
            int alto = bitmapOriginal.Height;
            float escala = Math.Min(1f, (float)maxLado / Math.Max(ancho, alto));

            SKBitmap bitmapParaSubir = bitmapOriginal;
            SKBitmap? bitmapRedimensionado = null;

            if (escala < 1f)
            {
                int nuevoAncho = Math.Max(1, (int)(ancho * escala));
                int nuevoAlto = Math.Max(1, (int)(alto * escala));

                bitmapRedimensionado = new SKBitmap(nuevoAncho, nuevoAlto);
                using (var canvas = new SKCanvas(bitmapRedimensionado))
                {
                    canvas.DrawBitmap(bitmapOriginal, new SKRect(0, 0, nuevoAncho, nuevoAlto));
                }
                bitmapParaSubir = bitmapRedimensionado;
            }

            using var imagenFinal = SKImage.FromBitmap(bitmapParaSubir);
            using var datosJpeg = imagenFinal.Encode(SKEncodedImageFormat.Jpeg, calidadJpeg);

            bitmapRedimensionado?.Dispose();

            return datosJpeg.ToArray();
        }
    }
}