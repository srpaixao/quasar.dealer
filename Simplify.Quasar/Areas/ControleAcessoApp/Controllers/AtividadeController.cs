using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Simplify.Quasar.Areas.ControleAcessoApp.ViewModels;
using Simplify.Quasar.Custom;
using Simplify.Quasar.Models;

namespace Simplify.Quasar.Areas.ControleAcessoApp.Controllers
{
    [ValidateSession]
    [AuthorizeFunction]
    public class AtividadeController : Controller
    {
        private readonly Quasar_Entities db = new Quasar_Entities();

        public ActionResult Index(bool atualizar = false)
        {
            var model = LoadActivities();

            if (atualizar)
            {
                return Json(new
                {
                    data = model.Select(item => new
                    {
                        item.Login,
                        item.Nome,
                        item.Filial,
                        item.Funcionalidade,
                        item.Rota,
                        UltimaAtividade = item.UltimaAtividade.ToString("dd/MM/yyyy HH:mm:ss"),
                        UltimaAtividadeOrdem = item.UltimaAtividade.ToString("yyyyMMddHHmmss")
                    }),
                    onlineCount = model.Count
                }, JsonRequestBehavior.AllowGet);
            }

            return View(model);
        }

        private IList<AtividadeViewModel> LoadActivities()
        {
            IList<OnlineUserActivity> atividades = OnlineUserTracker.GetActiveSessions();
            int[] usuarioIds = atividades.Select(x => x.UserId).Distinct().ToArray();

            var usuarios = (from u in db.Usuario
                            where usuarioIds.Contains(u.Id)
                            select new
                            {
                                u.Id,
                                u.Login,
                                u.Nome,
                                Filial = (from e in db.Empresa
                                          where e.Id == u.FilialId
                                          select e.Nome).FirstOrDefault()
                            }).ToList();
            var usuariosPorId = usuarios.ToDictionary(x => x.Id);

            var menus = db.AppMenu
                .Where(m => m.Status)
                .Select(m => new
                {
                    m.Titulo,
                    m.Area,
                    m.Controller,
                    m.Action
                })
                .ToList();

            return atividades
            .Where(atividade => usuariosPorId.ContainsKey(atividade.UserId))
            .Select(atividade =>
            {
                var usuario = usuariosPorId[atividade.UserId];
                string rota = BuildRoute(atividade.Area, atividade.Controller, atividade.Action);

                var menu = menus.FirstOrDefault(m =>
                    SameRoutePart(m.Area, atividade.Area)
                    && SameRoutePart(m.Controller, atividade.Controller)
                    && SameRoutePart(m.Action, atividade.Action));

                string funcionalidade = OnlineUserTracker.ResolveFunctionalityName(
                    atividade.Area,
                    atividade.Controller,
                    atividade.Action,
                    atividade.Functionality);

                return new AtividadeViewModel
                {
                    SessionId = atividade.SessionId,
                    UsuarioId = usuario.Id,
                    Login = usuario.Login,
                    Nome = usuario.Nome,
                    Filial = usuario.Filial,
                    Funcionalidade = !string.IsNullOrWhiteSpace(funcionalidade)
                        ? funcionalidade
                        : menu != null && !string.IsNullOrWhiteSpace(menu.Titulo)
                            ? menu.Titulo
                            : rota,
                    Rota = rota,
                    LoginEm = Util.ConvertUtcToApplicationTime(atividade.LoginAtUtc),
                    UltimaAtividade = Util.ConvertUtcToApplicationTime(atividade.ActivityAtUtc)
                };
            })
            .OrderByDescending(item => item.UltimaAtividade)
            .ToList();
        }

        private static bool SameRoutePart(string left, string right)
        {
            return string.Equals(
                left ?? string.Empty,
                right ?? string.Empty,
                StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildRoute(string area, string controller, string action)
        {
            return string.Join(
                " / ",
                new[] { area, controller, action }
                    .Where(part => !string.IsNullOrWhiteSpace(part)));
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
