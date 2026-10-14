using System;
using System.Linq;
using System.Threading.Tasks;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace PassGold.Services;

// Vista mínima de la tabla "usuarios": solo las 2 columnas de avisos.
// Se usa una clase aparte para NO tener que modificar tu Usuario.cs.
[Table("usuarios")]
public class PreferenciasAviso : BaseModel
{
    [PrimaryKey("id", false)]
    public string Id { get; set; } = string.Empty;

    [Column("avisos_cercania")]
    public bool AvisosCercania { get; set; } = false;

    [Column("distancia_aviso_metros")]
    public int DistanciaAvisoMetros { get; set; } = 100;
}

public static class PreferenciasAvisoService
{
    public static async Task<PreferenciasAviso?> ObtenerAsync(string userId)
    {
        try
        {
            var client = SupabaseService.Instance.Client;
            if (client == null || string.IsNullOrEmpty(userId)) return null;

            var resp = await client.From<PreferenciasAviso>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, userId)
                .Get();
            return resp.Models?.FirstOrDefault();
        }
        catch { return null; }
    }

    public static async Task<bool> GuardarAsync(string userId, bool avisos, int metros)
    {
        try
        {
            var client = SupabaseService.Instance.Client;
            if (client == null || string.IsNullOrEmpty(userId)) return false;

            await client.From<PreferenciasAviso>().Update(new PreferenciasAviso
            {
                Id = userId,
                AvisosCercania = avisos,
                DistanciaAvisoMetros = metros
            });
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Guardar preferencias de aviso falló: {ex.Message}");
            return false;
        }
    }
}