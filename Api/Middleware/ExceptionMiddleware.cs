using Microsoft.AspNetCore.Http;
using System.Net;
using System.Text.Json;

namespace QuasarApi.Middleware
{
    public class ExceptionMiddleware(RequestDelegate next)
    {
        private readonly RequestDelegate _next = next;

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // Log do erro (opcional: substituir por um sistema de logging como Serilog)
            Console.WriteLine($"Erro: {exception.Message}");

            // Configura a resposta HTTP para o cliente
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var response = new
            {
                StatusCode = context.Response.StatusCode,
                Message = "Ocorreu um erro no servidor. Tente novamente mais tarde.",
                Detailed = exception.Message // Opcional: pode ser removido em produção
            };

            // Retorna a resposta em JSON
            return context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}

