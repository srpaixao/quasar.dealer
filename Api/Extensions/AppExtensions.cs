using QuasarApi.Middleware;

namespace QuasarApi.Extensions
{
    public static class AppExtensions
    {
        public static void UseArchitectures(this WebApplication app)
        {
            app.MapControllers();
        }

        public static void UseServices(this WebApplication app)
        {
            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseCors("CorsPolicy");

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseMiddleware<ExceptionMiddleware>();
        }
    }
}
