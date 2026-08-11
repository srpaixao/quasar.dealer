using System;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Data.Entity.Infrastructure;

using System.Linq;
using System.Web.Mvc;

using Simplify.Quasar.Models;
using Simplify.Quasar.Custom;
using Simplify.Quasar.Areas.ControleAcessoApp.ViewModels;

namespace Simplify.Quasar.Areas.ControleAcessoApp.Controllers
{
    [ValidateSession]
    [AuthorizeFunction]
    public class UsuarioController : Controller
    {
        Quasar_Entities db = new Quasar_Entities();

        int filialId = Util.GetCurrentFilial();

        private const string AdminProfileMessage = "Somente o usuário admin pode atribuir o perfil admin.";

        // GET: Estoque/Contagem
        public ActionResult Index()
        {
            var atividades = OnlineUserTracker.GetLatestActivities();
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

            var vm = (from u in db.Usuario
                      where u.Login.ToLower() != "admin"
                      select new UsuarioViewModel
                      {
                          Id = u.Id,
                          Login = u.Login,
                          Nome = u.Nome,
                          NomePerfil = (from p in db.PerfilUsuario where p.Id == u.PerfilId select p.Descricao).FirstOrDefault(),
                          Email = u.Email,
                          Telefone = u.Telefone,
                          EmpresaId = u.EmpresaId ?? 0,
                          NomeEmpresa = string.Empty,
                          AreaId = u.AreaId ?? 0,
                          NomeArea = (from a in db.Area where a.Id == u.AreaId select a.Nome).FirstOrDefault(),
                          //FuncaoId = u.FuncaoId ?? 0,
                          //NomeFuncao = (from a in db.Funcao where a.Id == u.FuncaoId select a.Nome).FirstOrDefault(),
                          SenhaExpirada = u.SenhaExpirada,
                          AcessoBloqueado = u.AcessoBloqueado,
                          UltimoAcesso = u.UltimoAcesso,
                          CriadoEm = u.CriadoEm,
                          CriadoPor = u.CriadoPor,
                          ModificadoEm = u.ModificadoEm,
                          ModificadoPor = u.ModificadoPor
                      }).ToList();

            foreach (UsuarioViewModel usuario in vm)
            {
                OnlineUserActivity atividade;
                if (!atividades.TryGetValue(usuario.Id, out atividade))
                {
                    continue;
                }

                usuario.UsuarioLogado = true;
                usuario.UltimaAtividade = Util.ConvertUtcToApplicationTime(atividade.ActivityAtUtc);
                usuario.RotaAtual = BuildRoute(atividade.Area, atividade.Controller, atividade.Action);

                var menu = menus.FirstOrDefault(m =>
                    SameRoutePart(m.Area, atividade.Area)
                    && SameRoutePart(m.Controller, atividade.Controller)
                    && SameRoutePart(m.Action, atividade.Action));

                string funcionalidade = OnlineUserTracker.ResolveFunctionalityName(
                    atividade.Area,
                    atividade.Controller,
                    atividade.Action,
                    atividade.Functionality);

                usuario.FuncionalidadeAtual = !string.IsNullOrWhiteSpace(funcionalidade)
                    ? funcionalidade
                    : menu != null && !string.IsNullOrWhiteSpace(menu.Titulo)
                        ? menu.Titulo
                        : usuario.RotaAtual;
            }

            // Obtem lista de permissões
            //   ViewBag.Permissoes = Util.GetPermissoes(ControllerContext.RouteData.Values["controller"].ToString(), ControllerContext.RouteData.DataTokens["area"] as string);
            return View(vm);
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

        public ActionResult Create()
        {
            UsuarioViewModel vm = new UsuarioViewModel();
            vm.EmpresaId = Util.GetEmpresaSorocabaId();
            vm.EmpresaDDL = Util.GetEmpresas(vm.EmpresaId);
            //vm.AreaDDL = Util.GetAreas(null);
            vm.PerfilDDL = Util.GetPerfisUsuario(null, Util.IsAdminUser());
            return PartialView("_Create", vm);
        }

        // POST: Usuario/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(UsuarioViewModel vm)
        {

            if (!ModelState.IsValid)
            {
                vm.EmpresaDDL = Util.GetEmpresas(vm.EmpresaId);
                //vm.AreaDDL = Util.GetAreas(vm.AreaId);
                vm.PerfilDDL = Util.GetPerfisUsuario(vm.PerfilId, Util.IsAdminUser());
                return PartialView("_Create", vm);
            }

            if (!Util.IsAdminUser() && vm.PerfilId == 1)
            {
                ModelState.AddModelError("PerfilId", AdminProfileMessage);
                vm.EmpresaDDL = Util.GetEmpresas(vm.EmpresaId);
                vm.PerfilDDL = Util.GetPerfisUsuario(null, false);
                return PartialView("_Create", vm);
            }

            // Verifica se o login informado já existe na empresa
            if (db.Usuario.Any(p => p.Login == vm.Login))
            {
                ModelState.AddModelError("Login", "Já existe um usuário cadastrado com este login");
                vm.EmpresaDDL = Util.GetEmpresas(vm.EmpresaId);
                vm.AreaDDL = Util.GetAreas(vm.FilialId, vm.AreaId);
                vm.PerfilDDL = Util.GetPerfisUsuario(vm.PerfilId, Util.IsAdminUser());
                return PartialView("_Create", vm);
            }

            Usuario usuario = new Usuario();
            usuario.Login = vm.Login.ToUpper();
            usuario.PerfilId = vm.PerfilId;
            usuario.Senha = Util.HashPassword("123456");
            usuario.Nome = vm.Nome;
            usuario.Email = vm.Email;
            usuario.Telefone = vm.Telefone;
            usuario.EmpresaId = vm.EmpresaId;
            usuario.FilialId = vm.EmpresaId;
            //usuario.FuncaoId = vm.FuncaoId;
            usuario.SenhaExpirada = true;
            usuario.AcessoBloqueado = false;
            usuario.CriadoPor = Util.GetCurrentUser();
            usuario.CriadoEm = Util.GetCurrentDateTime();

            ViewBag.ControllerName = ControllerContext.RouteData.Values["controller"].ToString();
            ViewBag.ActionName = ControllerContext.RouteData.Values["action"].ToString();

            using (DbContextTransaction tr = db.Database.BeginTransaction())
            {
                try
                {
                    db.Usuario.Add(usuario);
                    db.SaveChanges();
                    tr.Commit();
                }
                catch (DbEntityValidationException ex)
                {
                    ViewBag.Exception = ex.Message;
                    ViewBag.InnerException = ex.InnerException;
                    ViewBag.Source = ex.Source;

                    string msgErro = "";
                    foreach (DbEntityValidationResult item in ex.EntityValidationErrors)
                    {
                        DbEntityEntry entry = item.Entry;
                        string entityTypeName = entry.Entity.GetType().Name;
                        foreach (DbValidationError subItem in item.ValidationErrors)
                        {
                            msgErro += string.Format("{0}", subItem.ErrorMessage + Environment.NewLine);
                        }
                    }
                    TempData["ErrorDetail"] = msgErro;

                    tr.Rollback();
                    return PartialView("_Create", vm);
                }
                catch (Exception ex)
                {
                    ViewBag.Exception = ex.Message;
                    ViewBag.InnerException = ex.InnerException;
                    ViewBag.Source = ex.Source;
                    tr.Rollback();
                    return PartialView("_Create", vm);
                }
            }

            return Json(new { success = true });
        }


        // GET: Usuario/Edit
        public ActionResult Edit(int id)
        {
            Usuario usuario = db.Usuario.Find(id);
            if (usuario == null)
            {
                return HttpNotFound();
            }

            UsuarioViewModel vm = new UsuarioViewModel();
            vm.Id = usuario.Id;
            vm.Login = usuario.Login;
            vm.PerfilId = usuario.PerfilId;
            vm.PerfilSomenteLeitura = !Util.IsAdminUser() && usuario.PerfilId == 1;
            vm.PerfilDDL = Util.GetPerfisUsuario(usuario.PerfilId, Util.IsAdminUser() || usuario.PerfilId == 1);
            vm.Nome = usuario.Nome;
            vm.Email = usuario.Email;
            vm.Telefone = usuario.Telefone;
            vm.EmpresaId = usuario.FilialId ?? usuario.EmpresaId;
            vm.FilialId = usuario.FilialId;
            vm.EmpresaDDL = Util.GetEmpresas(vm.EmpresaId);
            //vm.FuncaoId = usuario.FuncaoId;
            //vm.NomeFuncao = (from a in db.Funcao where a.Id == usuario.FuncaoId select a.Nome).FirstOrDefault();
            vm.SenhaExpirada = usuario.SenhaExpirada;
            vm.SenhaGerada = string.Empty;
            vm.UltimoAcesso = usuario.UltimoAcesso;
            vm.AcessoBloqueado = usuario.AcessoBloqueado;
            vm.CriadoPor = usuario.CriadoPor;
            vm.CriadoPorNome = usuario.CriadoPor;
            vm.CriadoPorNome  = (from u in db.Usuario where u.Login == usuario.CriadoPor select u.Nome).FirstOrDefault();
            vm.CriadoEm = usuario.CriadoEm;
            vm.ModificadoPor = usuario.ModificadoPor;
            vm.ModificadoPorNome = (from u in db.Usuario where u.Login == usuario.ModificadoPor select u.Nome).FirstOrDefault();
            vm.ModificadoEm = usuario.ModificadoEm;

            return PartialView("_Edit", vm);
        }

        // POST: Usuario/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(UsuarioViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.EmpresaDDL = Util.GetEmpresas(vm.EmpresaId);
                //vm.AreaDDL = Util.GetAreas(vm.AreaId);
                vm.PerfilDDL = Util.GetPerfisUsuario(vm.PerfilId, Util.IsAdminUser() || vm.PerfilId == 1);
                vm.PerfilSomenteLeitura = !Util.IsAdminUser() && vm.PerfilId == 1;
                return PartialView("_Edit", vm);
            }

            Usuario usuario = db.Usuario.Find(vm.Id);
            if (usuario == null)
            {
                return HttpNotFound();
            }

            if (!Util.IsAdminUser()
                && vm.PerfilId != usuario.PerfilId
                && (vm.PerfilId == 1 || usuario.PerfilId == 1))
            {
                ModelState.AddModelError("PerfilId", AdminProfileMessage);
                vm.PerfilId = usuario.PerfilId;
                vm.EmpresaDDL = Util.GetEmpresas(vm.EmpresaId);
                vm.PerfilDDL = Util.GetPerfisUsuario(usuario.PerfilId, Util.IsAdminUser() || usuario.PerfilId == 1);
                vm.PerfilSomenteLeitura = !Util.IsAdminUser() && usuario.PerfilId == 1;
                return PartialView("_Edit", vm);
            }

            usuario.PerfilId = vm.PerfilId;
            usuario.Nome = vm.Nome;
            usuario.Email = vm.Email;
            usuario.Telefone = vm.Telefone;
            usuario.EmpresaId = vm.EmpresaId;
            usuario.FilialId = vm.EmpresaId;
            //usuario.FuncaoId = vm.FuncaoId;
            //usuario.NomeFuncao = (from a in db.Funcao where a.Id == vm.FuncaoId select a.Nome).FirstOrDefault();
            usuario.AcessoBloqueado = vm.AcessoBloqueado;
            usuario.SenhaExpirada = vm.SenhaExpirada;

            if (vm.SenhaGerada != null && vm.SenhaGerada != string.Empty)
            {
                usuario.Senha = Util.HashPassword(vm.SenhaGerada);
                usuario.SenhaExpirada = true;
            }

            usuario.ModificadoPor = Util.GetCurrentUser();
            usuario.ModificadoEm = Util.GetCurrentDateTime();
            db.Entry(usuario).State = EntityState.Modified;

            ViewBag.ControllerName = ControllerContext.RouteData.Values["controller"].ToString();
            ViewBag.ActionName = ControllerContext.RouteData.Values["action"].ToString();

            using (DbContextTransaction tr = db.Database.BeginTransaction())
            {
                try
                {
                    db.SaveChanges();
                    tr.Commit();
                }
                catch (DbEntityValidationException ex)
                {
                    ViewBag.Exception = ex.Message;
                    ViewBag.InnerException = ex.InnerException;
                    ViewBag.Source = ex.Source;

                    string msgErro = "";
                    foreach (DbEntityValidationResult item in ex.EntityValidationErrors)
                    {
                        DbEntityEntry entry = item.Entry;
                        string entityTypeName = entry.Entity.GetType().Name;
                        foreach (DbValidationError subItem in item.ValidationErrors)
                        {
                            msgErro += string.Format("{0}", subItem.ErrorMessage + Environment.NewLine);
                        }
                    }
                    TempData["ErrorDetail"] = msgErro;

                    tr.Rollback();
                    return PartialView("_Edit", vm);
                }
                catch (Exception ex)
                {
                    ViewBag.Exception = ex.Message;
                    ViewBag.InnerException = ex.InnerException;
                    ViewBag.Source = ex.Source;
                    tr.Rollback();
                    return PartialView("_Edit", vm);
                }
            }

            return Json(new { success = true });
        }

        // POST: Usuario/Delete (chamada AJAX)
        [HttpPost]
        public ActionResult Delete(int id)
        {
            Usuario usuario = db.Usuario.Find(id);
            if (usuario == null)
            {
                return Json(new { success = false, msg = "Usuario não encontrado!" });
            }

            using (DbContextTransaction tr = db.Database.BeginTransaction())
            {
                try
                {
                    db.Usuario.Remove(usuario);
                    db.SaveChanges();
                    tr.Commit();
                }
                catch (DbEntityValidationException ex)
                {
                    string msgErro = "";
                    foreach (DbEntityValidationResult item in ex.EntityValidationErrors)
                    {
                        DbEntityEntry entry = item.Entry;
                        string entityTypeName = entry.Entity.GetType().Name;
                        foreach (DbValidationError subItem in item.ValidationErrors)
                        {
                            msgErro += string.Format("{0}", subItem.ErrorMessage + Environment.NewLine);
                        }
                    }
                    tr.Rollback();
                    return Json(new { success = false, msg = msgErro });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, msg = ex.Message });
                }
            }

            return Json(new { success = true, msg = "Operação realizada com sucesso" });
        }

        // GET: Usuario/Detail
        public ActionResult Detail(int id)
        {
            Usuario usuario = db.Usuario.Find(id);
            if (usuario == null)
            {
                return HttpNotFound();
            }

            var vm = (from u in db.Usuario
                      join e in db.Empresa on u.EmpresaId equals e.Id
                      join p in db.PerfilUsuario on u.PerfilId equals p.Id
                      where u.Id == id
                      select new UsuarioViewModel
                      {
                          Id = u.Id,
                          Login = u.Login,
                          Nome = u.Nome,
                          NomePerfil = p.Descricao,
                          Email = u.Email,
                          Telefone = u.Telefone,
                          EmpresaId = u.EmpresaId,
                          NomeEmpresa = e.Nome,
                          //FuncaoId = u.FuncaoId,
                          SenhaExpirada = u.SenhaExpirada,
                          AcessoBloqueado = u.AcessoBloqueado,
                          UltimoAcesso = u.UltimoAcesso,
                          CriadoEm = u.CriadoEm,
                          CriadoPor = u.CriadoPor,
                          ModificadoEm = u.ModificadoEm,
                          ModificadoPor = u.ModificadoPor
                      }).FirstOrDefault();

            if (vm == null)
            {
                return HttpNotFound();
            }

            return PartialView("_Detail", vm);
        }

        // GET: Usuario/EditProfile
        public ActionResult EditProfile(int id)
        {
            Usuario usuario = db.Usuario.Find(id);
            if (usuario == null)
            {
                return HttpNotFound();
            }

            UsuarioViewModel vm = new UsuarioViewModel();
            vm.Id = usuario.Id;
            vm.Login = usuario.Login;
            vm.Senha = usuario.Senha;
            vm.Nome = usuario.Nome;
            vm.Email = usuario.Email;
            vm.Telefone = usuario.Telefone;
            //vm.FuncaoId = usuario.FuncaoId;

            vm.NomeEmpresa = (from e in db.Empresa where e.Id == usuario.EmpresaId select e.Nome).FirstOrDefault();

            return PartialView("_EditProfile", vm);
        }

        // POST: Usuario/EditProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditProfile(UsuarioViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return PartialView("_EditProfile", vm);
            }

            Usuario usuario = db.Usuario.Find(vm.Id);
            if (usuario == null)
            {
                return HttpNotFound();
            }

            usuario.Nome = vm.Nome;
            usuario.Email = vm.Email;
            usuario.Telefone = vm.Telefone;

            if (vm.Senha != null && vm.Senha != string.Empty)
            {
                usuario.Senha = Util.HashPassword(vm.Senha);
            }

            usuario.ModificadoEm = Util.GetCurrentDateTime();
            usuario.ModificadoPor = Util.GetCurrentUser();

            db.Entry(usuario).State = EntityState.Modified;

            ViewBag.ControllerName = "Usuario";
            ViewBag.ActionName = "EditProfile";

            using (DbContextTransaction tr = db.Database.BeginTransaction())
            {
                try
                {
                    db.SaveChanges();
                    tr.Commit();
                    Session["displayname"] = usuario.Nome;
                }
                catch (DbEntityValidationException ex)
                {
                    ViewBag.Exception = ex.Message;
                    ViewBag.InnerException = ex.InnerException;
                    ViewBag.Source = ex.Source;

                    string msgErro = "";
                    foreach (DbEntityValidationResult item in ex.EntityValidationErrors)
                    {
                        DbEntityEntry entry = item.Entry;
                        string entityTypeName = entry.Entity.GetType().Name;
                        foreach (DbValidationError subItem in item.ValidationErrors)
                        {
                            msgErro += string.Format("{0}", subItem.ErrorMessage + Environment.NewLine);
                        }
                    }
                    TempData["ErrorDetail"] = msgErro;

                    tr.Rollback();
                    return PartialView("_EditProfile", vm);
                }
                catch (Exception ex)
                {
                    ViewBag.Exception = ex.Message;
                    ViewBag.InnerException = ex.InnerException;
                    ViewBag.Source = ex.Source;
                    tr.Rollback();
                    return PartialView("_EditProfile", vm);
                }
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
