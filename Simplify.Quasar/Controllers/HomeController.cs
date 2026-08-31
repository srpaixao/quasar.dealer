using System.Web.Mvc;
using System.Linq;
using System.Data.SqlClient;
using Simplify.Quasar.Models;
using Simplify.Quasar.Custom;

namespace Simplify.Quasar.Controllers
{
    [ValidateSession]
    public class HomeController : Controller
    {
        private const int StatusAguardandoSeparacao = 2;
        Quasar_Entities db = new Quasar_Entities();

        int filialId = Util.GetCurrentFilial();

        [HttpGet]
        public ActionResult Index()
        {
            // Buscar filial informada no login
            int filialid = Util.GetCurrentFilial();
            if (filialid == 0) 
            {
                return RedirectToAction("Logout", "Account");
            }              

            ViewBag.filialid = filialid;
            ViewBag.VolumesParaConferencia = db.Volume.Where(x => x.StatusId == 1 && x.FilialId == filialid).Count();
            ViewBag.RomaneiosParaSeparacao = db.Romaneio.Count(x => x.StatusId == StatusAguardandoSeparacao && x.FilialId == filialid);
            int statusPendenteDevolucaoId = db.StatusDevolucao
                .ToList()
                .Where(x => Util.RemoverAcentuacao((x.Nome ?? string.Empty).Trim()).ToUpperInvariant() == "PENDENTE")
                .Select(x => x.Id)
                .FirstOrDefault();

            ViewBag.DevolucoesPendentes = statusPendenteDevolucaoId == 0
                ? 0
                : db.Devolucao.Count(x => x.FilialId == filialid && x.StatusId == statusPendenteDevolucaoId);
            ViewBag.ItensAnomaliasPendentes = db.Database.SqlQuery<int>(@"
IF OBJECT_ID('dbo.AnomaliaGmItem', 'U') IS NULL
   OR OBJECT_ID('dbo.AnomaliaGmProcesso', 'U') IS NULL
   OR OBJECT_ID('dbo.AnomaliaGmStatus', 'U') IS NULL
BEGIN
    SELECT 0;
END
ELSE
BEGIN
    EXEC sys.sp_executesql N'
        SELECT COUNT(*)
        FROM dbo.AnomaliaGmItem item
        INNER JOIN dbo.AnomaliaGmProcesso processo ON processo.Id = item.AnomaliaId
        INNER JOIN dbo.AnomaliaGmStatus status ON status.Id = item.StatusId
        WHERE item.FilialId = @filialId
          AND processo.FilialId = @filialId
          AND item.Cancelado = 0
          AND processo.Cancelado = 0
          AND processo.Ativo = 1
          AND status.Codigo = ''EM_PROCESSO'';',
        N'@filialId int',
        @filialId;
END",
                new SqlParameter("@filialId", filialid)).Single();
            return View(); 
        }

        [HttpPost]
        public ActionResult RegistrarAtividade(
            string functionality,
            string activityArea,
            string activityController,
            string activityAction)
        {
            string menuTitle = db.AppMenu
                .Where(m =>
                    m.Status
                    && (m.Area ?? string.Empty) == (activityArea ?? string.Empty)
                    && (m.Controller ?? string.Empty) == (activityController ?? string.Empty)
                    && (m.Action ?? string.Empty) == (activityAction ?? string.Empty))
                .Select(m => m.Titulo)
                .FirstOrDefault();

            string resolvedFunctionality = !string.IsNullOrWhiteSpace(menuTitle)
                ? menuTitle
                : functionality;

            int userId;
            int currentFilialId;
            if (int.TryParse(System.Convert.ToString(Session["userid"]), out userId)
                && int.TryParse(System.Convert.ToString(Session["filialid"]), out currentFilialId))
            {
                OnlineUserTracker.Track(
                    Session.SessionID,
                    userId,
                    currentFilialId,
                    Session.Timeout,
                    activityArea,
                    activityController,
                    activityAction,
                    resolvedFunctionality);
            }

            return Json(new { success = true });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }

            base.Dispose(disposing);
        }

    }
}
