using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using Simplify.Quasar.Models;

namespace Simplify.Quasar.Custom
{
    public static class OnlineUserTracker
    {
        private const int DefaultActivityTimeoutMinutes = 2;
        private static readonly object SchemaLock = new object();
        private static readonly ConcurrentDictionary<int, TimeoutCacheEntry> TimeoutByFilial =
            new ConcurrentDictionary<int, TimeoutCacheEntry>();
        private static bool SchemaInitialized;

        public static void Track(string sessionId, int userId, int filialId, int timeoutMinutes)
        {
            Track(sessionId, userId, filialId, timeoutMinutes, null, null, null, null);
        }

        public static void Track(
            string sessionId,
            int userId,
            int filialId,
            int timeoutMinutes,
            string area,
            string controller,
            string action)
        {
            Track(sessionId, userId, filialId, timeoutMinutes, area, controller, action, null);
        }

        public static void Track(
            string sessionId,
            int userId,
            int filialId,
            int timeoutMinutes,
            string area,
            string controller,
            string action,
            string functionality)
        {
            if (string.IsNullOrWhiteSpace(sessionId) || userId <= 0)
            {
                return;
            }

            functionality = ResolveFunctionalityName(area, controller, action, functionality);
            bool routeWasProvided = !string.IsNullOrWhiteSpace(area)
                || !string.IsNullOrWhiteSpace(controller)
                || !string.IsNullOrWhiteSpace(action);
            HttpRequest request = HttpContext.Current != null ? HttpContext.Current.Request : null;

            try
            {
                using (var db = new Quasar_Entities())
                {
                    EnsureSchema(db);
                    db.Database.ExecuteSqlCommand(
                        @"
UPDATE UsuarioSessaoAtiva
SET
    UsuarioId = @UsuarioId,
    FilialId = @FilialId,
    LoginEm = CASE
        WHEN UsuarioId <> @UsuarioId OR FilialId <> @FilialId THEN @Agora
        ELSE LoginEm
    END,
    UltimaAtividadeEm = @Agora,
    LogoutEm = NULL,
    Ativo = 1,
    EnderecoIP = COALESCE(NULLIF(@EnderecoIP, ''), EnderecoIP),
    UserAgent = COALESCE(NULLIF(@UserAgent, ''), UserAgent),
    Area = CASE WHEN @RotaInformada = 1 THEN @Area ELSE Area END,
    Controller = CASE WHEN @RotaInformada = 1 THEN @Controller ELSE Controller END,
    Action = CASE WHEN @RotaInformada = 1 THEN @Action ELSE Action END,
    Funcionalidade = CASE
        WHEN @Funcionalidade IS NOT NULL THEN @Funcionalidade
        WHEN @RotaInformada = 1 THEN NULL
        ELSE Funcionalidade
    END
WHERE SessionId = @SessionId;

IF @@ROWCOUNT = 0
BEGIN
    INSERT INTO UsuarioSessaoAtiva
    (
        UsuarioId, SessionId, FilialId, LoginEm, UltimaAtividadeEm,
        LogoutEm, Ativo, EnderecoIP, UserAgent, Area, Controller,
        Action, Funcionalidade
    )
    VALUES
    (
        @UsuarioId, @SessionId, @FilialId, @Agora, @Agora,
        NULL, 1, @EnderecoIP, @UserAgent, @Area, @Controller,
        @Action, @Funcionalidade
    );
END",
                        new SqlParameter("@UsuarioId", userId),
                        new SqlParameter("@SessionId", sessionId),
                        new SqlParameter("@FilialId", filialId),
                        new SqlParameter("@Agora", DateTime.UtcNow),
                        new SqlParameter("@EnderecoIP", (object)(request != null ? request.UserHostAddress : null) ?? DBNull.Value),
                        new SqlParameter("@UserAgent", (object)(request != null ? request.UserAgent : null) ?? DBNull.Value),
                        new SqlParameter("@Area", (object)area ?? DBNull.Value),
                        new SqlParameter("@Controller", (object)controller ?? DBNull.Value),
                        new SqlParameter("@Action", (object)action ?? DBNull.Value),
                        new SqlParameter("@Funcionalidade", (object)functionality ?? DBNull.Value),
                        new SqlParameter("@RotaInformada", routeWasProvided));
                }
            }
            catch
            {
                // O rastreamento não pode interromper a requisição funcional do usuário.
            }
        }

        public static void Unregister(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return;
            }

            try
            {
                using (var db = new Quasar_Entities())
                {
                    EnsureSchema(db);
                    db.Database.ExecuteSqlCommand(
                        @"UPDATE UsuarioSessaoAtiva
                          SET Ativo = 0, LogoutEm = @p0
                          WHERE SessionId = @p1 AND Ativo = 1",
                        DateTime.UtcNow,
                        sessionId);
                }
            }
            catch
            {
            }
        }

        public static HashSet<int> GetOnlineUserIds()
        {
            return new HashSet<int>(GetActiveSessions().Select(x => x.UserId));
        }

        public static IDictionary<int, OnlineUserActivity> GetLatestActivities()
        {
            return GetActiveSessions()
                .GroupBy(x => x.UserId)
                .ToDictionary(
                    x => x.Key,
                    x => x.OrderByDescending(item => item.ActivityAtUtc).First());
        }

        public static IList<OnlineUserActivity> GetActiveSessions()
        {
            try
            {
                using (var db = new Quasar_Entities())
                {
                    EnsureSchema(db);
                    List<OnlineUserActivity> sessions = db.Database.SqlQuery<OnlineUserActivity>(
                        @"SELECT
                              SessionId,
                              UsuarioId AS UserId,
                              FilialId,
                              LoginEm AS LoginAtUtc,
                              UltimaAtividadeEm AS ActivityAtUtc,
                              Area,
                              Controller,
                              Action,
                              Funcionalidade AS Functionality,
                              EnderecoIP AS IpAddress,
                              UserAgent
                          FROM UsuarioSessaoAtiva
                          WHERE Ativo = 1").ToList();

                    DateTime nowUtc = DateTime.UtcNow;
                    List<OnlineUserActivity> active = sessions
                        .Where(x => x.ActivityAtUtc >= nowUtc.AddMinutes(-ResolveActivityTimeoutMinutes(x.FilialId)))
                        .ToList();
                    List<string> expiredSessionIds = sessions
                        .Where(x => x.ActivityAtUtc < nowUtc.AddMinutes(-ResolveActivityTimeoutMinutes(x.FilialId)))
                        .Select(x => x.SessionId)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .ToList();

                    if (expiredSessionIds.Count > 0)
                    {
                        foreach (string expiredSessionId in expiredSessionIds)
                        {
                            db.Database.ExecuteSqlCommand(
                                @"UPDATE UsuarioSessaoAtiva
                                  SET Ativo = 0
                                  WHERE SessionId = @p0 AND Ativo = 1",
                                expiredSessionId);
                        }
                    }

                    return active;
                }
            }
            catch
            {
                return new List<OnlineUserActivity>();
            }
        }

        public static string ResolveFunctionalityName(
            string area,
            string controller,
            string action,
            string fallback = null)
        {
            if (SameRoute(area, controller, action, "ExpedicaoApp", "NotaFiscal", "PrintExpedicaoManual"))
            {
                return "Imprimir Etiquetas";
            }

            if (SameRoute(area, controller, action, "ExpedicaoApp", "NotaFiscal", "PrintTransportadoraManual"))
            {
                return "Importar arquivo de Transportadora";
            }

            if (SameRoute(area, controller, action, "DevolucaoApp", "Home", "Print"))
            {
                return "Cadastro de Devolução";
            }

            return fallback;
        }

        private static int ResolveActivityTimeoutMinutes(int filialId)
        {
            DateTime nowUtc = DateTime.UtcNow;
            TimeoutCacheEntry cached;
            if (TimeoutByFilial.TryGetValue(filialId, out cached) && cached.ValidUntilUtc > nowUtc)
            {
                return cached.Minutes;
            }

            int minutes;
            try
            {
                minutes = Util.GetOnlineUserTimeoutMinutes(filialId);
            }
            catch
            {
                minutes = DefaultActivityTimeoutMinutes;
            }

            minutes = Math.Max(minutes, 1);
            TimeoutByFilial[filialId] = new TimeoutCacheEntry
            {
                Minutes = minutes,
                ValidUntilUtc = nowUtc.AddMinutes(1)
            };
            return minutes;
        }

        private static void EnsureSchema(Quasar_Entities db)
        {
            if (SchemaInitialized)
            {
                return;
            }

            lock (SchemaLock)
            {
                if (SchemaInitialized)
                {
                    return;
                }

                db.Database.ExecuteSqlCommand(
                    @"
IF OBJECT_ID('dbo.UsuarioSessaoAtiva', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.UsuarioSessaoAtiva
    (
        Id bigint IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_UsuarioSessaoAtiva PRIMARY KEY,
        UsuarioId int NOT NULL,
        SessionId nvarchar(128) NOT NULL,
        FilialId int NOT NULL,
        LoginEm datetime2 NOT NULL,
        UltimaAtividadeEm datetime2 NOT NULL,
        LogoutEm datetime2 NULL,
        Ativo bit NOT NULL
            CONSTRAINT DF_UsuarioSessaoAtiva_Ativo DEFAULT(1),
        EnderecoIP nvarchar(64) NULL,
        UserAgent nvarchar(512) NULL,
        Area nvarchar(128) NULL,
        Controller nvarchar(128) NULL,
        Action nvarchar(128) NULL,
        Funcionalidade nvarchar(256) NULL,
        CONSTRAINT UQ_UsuarioSessaoAtiva_SessionId UNIQUE(SessionId)
    );

    CREATE INDEX IX_UsuarioSessaoAtiva_Ativo_UltimaAtividade
        ON dbo.UsuarioSessaoAtiva(Ativo, UltimaAtividadeEm);
END");

                SchemaInitialized = true;
            }
        }

        private static bool SameRoute(
            string area,
            string controller,
            string action,
            string expectedArea,
            string expectedController,
            string expectedAction)
        {
            return string.Equals(area ?? string.Empty, expectedArea, StringComparison.OrdinalIgnoreCase)
                && string.Equals(controller ?? string.Empty, expectedController, StringComparison.OrdinalIgnoreCase)
                && string.Equals(action ?? string.Empty, expectedAction, StringComparison.OrdinalIgnoreCase);
        }

        private sealed class TimeoutCacheEntry
        {
            public int Minutes { get; set; }
            public DateTime ValidUntilUtc { get; set; }
        }
    }

    public sealed class OnlineUserActivity
    {
        public string SessionId { get; set; }
        public int UserId { get; set; }
        public int FilialId { get; set; }
        public DateTime LoginAtUtc { get; set; }
        public DateTime ActivityAtUtc { get; set; }
        public string Area { get; set; }
        public string Controller { get; set; }
        public string Action { get; set; }
        public string Functionality { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
    }
}
