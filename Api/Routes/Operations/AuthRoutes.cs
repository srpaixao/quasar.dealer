using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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
                // Validar usuário e senha
                var user = await db.Usuario.FirstOrDefaultAsync(u =>
                    EF.Functions.Like(u.Login, userCredentials.Usuario) &&
                    u.FilialId == userCredentials.FilialId);
                if (user == null)
                {
                    return Results.NotFound(new { mensagem = "Usuário não cadastrado" });
                }

                if (!CryptoHelper.ValidatePassword(userCredentials.Senha, user.Senha))
                {
                    return Results.Json(
                        new { mensagem = "Senha incorreta" },
                        statusCode: StatusCodes.Status401Unauthorized
                     );
                }

                //if (CryptoHelper.CryptoToString(user.Senha) != userCredentials.Senha)
                //{
                //    return Results.Json(
                //        new { mensagem = "Senha incorreta" },
                //        statusCode: StatusCodes.Status401Unauthorized
                //     );
                //}

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
                         new { mensagem = "Acesso Bloqueado" },
                         statusCode: StatusCodes.Status401Unauthorized
                      );
                }

                // Gerar token JWT
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!);

                var credentials = new SigningCredentials(new SymmetricSecurityKey(
                                        Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Key"]!)),
                                        SecurityAlgorithms.HmacSha256Signature);

                var ci = new ClaimsIdentity();
                ci.AddClaim(new Claim(ClaimTypes.Name, user.Login));

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = ci,
                    SigningCredentials = credentials,
                    Expires = DateTime.UtcNow.AddHours(1)
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = tokenHandler.WriteToken(token);

                // Enviar o token como um cookie HTTP-only
                //httpContext.Response.Cookies.Append("quasarJWT", tokenString, new CookieOptions
                //{
                //    HttpOnly = true,
                //    Secure = false, // Use true em produção (HTTPS)
                //    SameSite = SameSiteMode.Lax, // Use SameSiteMode.Strict em produção
                //    Path = "/", // Define o caminho para cobrir todas as requisições para o mesmo domínio
                //    Expires = DateTime.UtcNow.AddHours(1)
                //});

                //return Results.Ok(new { Message = "Login successful" });
                return Results.Ok(new
                {
                    useraccount = user.Login,
                    username = user.Nome,
                    email = user.Email,
                    filialId = userCredentials.FilialId,
                    token = tokenString,
                    message = "Login successful"
                });
            });

            //app.MapGet($"{route}/check-cookie", (HttpContext httpContext) =>
            //{
            //    if (httpContext.Request.Cookies.ContainsKey("quasarJWT"))
            //    {
            //        return Results.Ok(new { exists = true });
            //    }
            //    return Results.Ok(new { exists = false });
            //});

            //app.MapPost($"{route}/remove-cookie", (HttpContext httpContext) =>
            //{
            //    if (httpContext.Request.Cookies.ContainsKey("quasarJWT"))
            //    {
            //        httpContext.Response.Cookies.Append("quasarJWT", "", new CookieOptions
            //        {
            //            Expires = DateTime.UtcNow.AddDays(-1), // Definir expiração no passado
            //            HttpOnly = true,
            //            Secure = false, // Use true em produção (HTTPS)
            //            SameSite = SameSiteMode.Lax,
            //            Path = "/"
            //        });
            //        return Results.Ok(new { removed = true });
            //    }
            //    return Results.BadRequest(new { removed = false, message = "Cookie not found" });
            //});

            return app;
        }

    }
}
