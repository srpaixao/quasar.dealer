using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuasarApi.Database.Models;
using QuasarApi.DataBase;

namespace QuasarApi.Routes.Management
{
    public static class UsuarioRoutes
    {
        public static WebApplication MapUsuarioRoutes(this WebApplication app, WebApplicationBuilder builder)
        {
            // Obter lista de usuarios
            app.MapGet("/usuarios", async ([FromQuery] int? filialId, AppDbContext db) =>
            {
                return await db.Usuario
                    .Where(u => u.FilialId == filialId)
                    .Select(u => new { u.Id, u.Login, u.Nome, u.Email, u.SenhaExpirada, u.AcessoBloqueado, u.EmpresaId, u.FilialId })
                    .ToListAsync();
            }).RequireAuthorization();

            // Obter usuario
            app.MapGet("/usuarios/{id}", async (int id, [FromQuery] int? filialId, AppDbContext db) =>
            {
                var usuario = await db.Usuario
                                        .Where(u => u.Id == id && u.FilialId == filialId)
                                        .Select(u => new { u.Id, u.Login, u.Nome, u.Email, u.SenhaExpirada, u.AcessoBloqueado, u.EmpresaId, u.FilialId })
                                        .FirstOrDefaultAsync(); 
                
                return usuario != null ? Results.Ok(usuario) : Results.NotFound();
            }).RequireAuthorization();

            // Incluir usuário
            app.MapPost("/usuarios", async (Usuario usuario, AppDbContext db) =>
            {
                db.Usuario.Add(usuario);
                await db.SaveChangesAsync();

                return Results.Created($"/usuarios/{usuario.Id}", usuario);
            }).RequireAuthorization();

            // Modificar usuario (todas as colunas)
            app.MapPut("/usuarios/{id}", async (int id, [FromQuery] int? filialId, Usuario inputusuario, AppDbContext db) =>
            {
                var usuario = await db.Usuario.FirstOrDefaultAsync(u => u.Id == id && u.FilialId == filialId);

                if (usuario is null) return Results.NotFound();

                usuario.Login = inputusuario.Login;
                usuario.Senha = inputusuario.Senha;
                usuario.Nome = inputusuario.Nome;
                usuario.Email = inputusuario.Email;
                usuario.SenhaExpirada = inputusuario.SenhaExpirada;
                usuario.AcessoBloqueado = inputusuario.AcessoBloqueado;
                usuario.EmpresaId = inputusuario.EmpresaId;
                usuario.FilialId = inputusuario.FilialId;

                await db.SaveChangesAsync();

                return Results.NoContent();
            }).RequireAuthorization();

            // Modificar usuario (colunas específicas)
            app.MapPatch("/usuarios/{id}", async (int id, [FromQuery] int? filialId, JsonElement patchData, AppDbContext db) =>
            {
                var usuario = await db.Usuario.FirstOrDefaultAsync(u => u.Id == id && u.FilialId == filialId);

                if (usuario is null)
                {
                    return Results.NotFound();
                }

                foreach (var property in patchData.EnumerateObject())
                {
                    switch (property.Name.ToLower())
                    {
                        case "nome":
                            var nome = property.Value.GetString();
                            if (!string.IsNullOrEmpty(nome))
                            {
                                usuario.Nome = nome;
                            }
                            break;

                        case "email":
                            var email = property.Value.GetString();
                            if (!string.IsNullOrEmpty(email))
                            {
                                usuario.Email = email;
                            }
                            break;
                    }
                }

                await db.SaveChangesAsync();

                return Results.NoContent();
            }).RequireAuthorization();

            // Excluir usuario
            app.MapDelete("/usuarios/{id}", async (int id, [FromQuery] int? filialId, AppDbContext db) =>
            {
                var usuario = await db.Usuario.FirstOrDefaultAsync(u => u.Id == id && u.FilialId == filialId);
                if (usuario is not null)
                {
                    db.Usuario.Remove(usuario);
                    await db.SaveChangesAsync();
                    return Results.Ok(usuario);
                }

                return Results.NotFound();
            }).RequireAuthorization();

            return app;
        }
    }
}
