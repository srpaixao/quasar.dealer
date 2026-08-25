using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using QuasarApi.DataBase;
using QuasarApi.Database.Models;

namespace QuasarApi.Routes.Management
{
    public static class UsuarioRoutes
    {
        public static WebApplication MapUsuarioRoutes(this WebApplication app, WebApplicationBuilder builder)
        {
            app.MapGet("/usuarios", async (AppDbContext db) =>
            {
                return await db.Usuario
                    .Select(u => new { u.Id, u.Login, u.Nome, u.Email, u.SenhaExpirada, u.AcessoBloqueado, u.EmpresaId, u.FilialId })
                    .ToListAsync();
            }).RequireAuthorization();

            app.MapGet("/usuarios/{id}", async (int id, AppDbContext db) =>
            {
                var usuario = await db.Usuario
                    .Where(u => u.Id == id)
                    .Select(u => new { u.Id, u.Login, u.Nome, u.Email, u.SenhaExpirada, u.AcessoBloqueado, u.EmpresaId, u.FilialId })
                    .FirstOrDefaultAsync();

                return usuario != null ? Results.Ok(usuario) : Results.NotFound();
            }).RequireAuthorization();

            app.MapPost("/usuarios", async (Usuario usuario, AppDbContext db) =>
            {
                string login = NormalizeLogin(usuario.Login);
                if (string.IsNullOrWhiteSpace(login))
                {
                    return Results.BadRequest(new { mensagem = "Login obrigatorio." });
                }

                bool loginExiste = await db.Usuario.AnyAsync(u => u.Login.ToUpper() == login);
                if (loginExiste)
                {
                    return Results.Conflict(new { mensagem = "Ja existe um usuario cadastrado com este login." });
                }

                usuario.Login = login;
                db.Usuario.Add(usuario);
                await db.SaveChangesAsync();

                return Results.Created($"/usuarios/{usuario.Id}", usuario);
            }).RequireAuthorization();

            app.MapPut("/usuarios/{id}", async (int id, Usuario inputusuario, AppDbContext db) =>
            {
                var usuario = await db.Usuario.FindAsync(id);
                if (usuario is null)
                {
                    return Results.NotFound();
                }

                string login = NormalizeLogin(inputusuario.Login);
                if (string.IsNullOrWhiteSpace(login))
                {
                    return Results.BadRequest(new { mensagem = "Login obrigatorio." });
                }

                bool loginExiste = await db.Usuario.AnyAsync(u => u.Id != id && u.Login.ToUpper() == login);
                if (loginExiste)
                {
                    return Results.Conflict(new { mensagem = "Ja existe um usuario cadastrado com este login." });
                }

                usuario.Login = login;
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

            app.MapPatch("/usuarios/{id}", async (int id, JsonElement patchData, AppDbContext db) =>
            {
                var usuario = await db.Usuario.FindAsync(id);
                if (usuario is null)
                {
                    return Results.NotFound();
                }

                foreach (var property in patchData.EnumerateObject())
                {
                    switch (property.Name.ToLowerInvariant())
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

            app.MapDelete("/usuarios/{id}", async (int id, AppDbContext db) =>
            {
                if (await db.Usuario.FindAsync(id) is Usuario usuario)
                {
                    db.Usuario.Remove(usuario);
                    await db.SaveChangesAsync();
                    return Results.Ok(usuario);
                }

                return Results.NotFound();
            }).RequireAuthorization();

            return app;
        }

        private static string NormalizeLogin(string? login)
        {
            return (login ?? string.Empty).Trim().ToUpperInvariant();
        }
    }
}
