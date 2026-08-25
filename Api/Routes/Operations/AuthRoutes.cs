using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuasarApi.DataBase;
using QuasarApi.DTO.Operations.Auth;
using QuasarApi.Helpers;

namespace QuasarApi.Routes.Operations
{
    public static class AuthRoutes
    {
        public static WebApplication MapAuthRoutes(this WebApplication app, WebApplicationBuilder builder)
        {
            const string groupPrefix = "/auth";
            var group = app.MapGroup(groupPrefix);

            group.MapPost("/login", async (HttpContext httpContext, AppDbContext db, Login userCredentials) =>
            {
                string loginInformado = (userCredentials.Usuario ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(loginInformado))
                {
                    return Results.BadRequest(new { mensagem = "Usuario nao informado" });
                }

                var users = await db.Usuario
                    .Where(u => u.Login != null && u.Login.ToUpper() == loginInformado.ToUpper())
                    .OrderBy(u => u.Id)
                    .ToListAsync();

                if (!users.Any())
                {
                    return Results.NotFound(new { mensagem = "Usuario nao cadastrado" });
                }

                if (users.Count > 1)
                {
                    return Results.Json(
                        new { mensagem = "Existe mais de um usuario cadastrado com este login. Corrija o cadastro antes de acessar o coletor." },
                        statusCode: StatusCodes.Status409Conflict
                    );
                }

                var user = users[0];

                if (!CryptoHelper.ValidatePassword(userCredentials.Senha, user.Senha))
                {
                    return Results.Json(
                        new { mensagem = "Senha incorreta" },
                        statusCode: StatusCodes.Status401Unauthorized
                    );
                }

                if (user.SenhaExpirada)
                {
                    return Results.Json(
                        new { mensagem = "Senha expirada" },
                        statusCode: StatusCodes.Status401Unauthorized
                    );
                }

                if (user.AcessoBloqueado)
                {
                    return Results.Json(
                        new { mensagem = "Acesso bloqueado" },
                        statusCode: StatusCodes.Status401Unauthorized
                    );
                }

                var tokenHandler = new JwtSecurityTokenHandler();
                var credentials = new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Key"]!)),
                    SecurityAlgorithms.HmacSha256Signature);

                var ci = new ClaimsIdentity();
                ci.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
                ci.AddClaim(new Claim(ClaimTypes.Name, user.Login));

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = ci,
                    SigningCredentials = credentials,
                    Expires = DateTime.UtcNow.AddHours(1)
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = tokenHandler.WriteToken(token);

                return Results.Ok(new
                {
                    useraccount = user.Login,
                    username = user.Nome,
                    email = user.Email,
                    filialId = user.FilialId,
                    token = tokenString,
                    message = "Login successful"
                });
            });

            return app;
        }
    }
}
