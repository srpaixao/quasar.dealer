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
    public class PerfilController : Controller
    {
        Quasar_Entities db = new Quasar_Entities();
        int filialId = Util.GetCurrentFilial();
        private const string UnauthorizedProfileMessage = "Acesso não autorizado. Seu perfil não possui permissão para acessar esta funcionalidade.";

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (!Util.IsAdminProfile())
            {
                filterContext.Result = RedirectToAction("UnauthorizedError", "Error", new { area = "", message = UnauthorizedProfileMessage });
                return;
            }

            base.OnActionExecuting(filterContext);
        }

        // GET: Perfil
        public ActionResult Index()
        {
            var vm = (from p in db.PerfilUsuario
                      select new PerfilViewModel
                      {
                          Id = p.Id,
                          Nome = p.Nome,
                          Descricao = p.Descricao,
                          FilialId = p.FilialId,
                          NomeFilial = (from e in db.Empresa where e.Id == p.FilialId select e.Nome).FirstOrDefault(),
                          CriadoEm = p.CriadoEm,
                          CriadoPor = p.CriadoPor,
                          ModificadoEm = p.ModificadoEm,
                          ModificadoPor = p.ModificadoPor
                      }).ToList();

            return View(vm);
        }

        public ActionResult Create()
        {
            PerfilViewModel vm = new PerfilViewModel();
            vm.FilialDDL = Util.GetEmpresas(null);
            return PartialView("_Create", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(PerfilViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.FilialDDL = Util.GetEmpresas(vm.FilialId);
                return PartialView("_Create", vm);
            }

            if (db.PerfilUsuario.Any(p => p.Nome.ToLower() == vm.Nome.ToLower()))
            {
                ModelState.AddModelError("Nome", "Já existe um perfil com este nome");
                vm.FilialDDL = Util.GetEmpresas(vm.FilialId);
                return PartialView("_Create", vm);
            }

            PerfilUsuario perfil = new PerfilUsuario();
            perfil.Nome = vm.Nome;
            perfil.Descricao = vm.Descricao;
            perfil.FilialId = vm.FilialId;
            perfil.CriadoPor = Util.GetCurrentUser();
            perfil.CriadoEm = Util.GetCurrentDateTime();

            using (DbContextTransaction tr = db.Database.BeginTransaction())
            {
                try
                {
                    db.PerfilUsuario.Add(perfil);
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

        public ActionResult Edit(int id)
        {
            PerfilUsuario perfil = db.PerfilUsuario.Find(id);
            if (perfil == null)
            {
                return HttpNotFound();
            }

            PerfilViewModel vm = new PerfilViewModel();
            vm.Id = perfil.Id;
            vm.Nome = perfil.Nome;
            vm.Descricao = perfil.Descricao;
            vm.FilialId = perfil.FilialId;
            vm.FilialDDL = Util.GetEmpresas(perfil.FilialId);
            vm.CriadoPor = perfil.CriadoPor;
            vm.CriadoPorNome = (from u in db.Usuario where u.Login == perfil.CriadoPor select u.Nome).FirstOrDefault();
            vm.CriadoEm = perfil.CriadoEm;
            vm.ModificadoPor = perfil.ModificadoPor;
            vm.ModificadoPorNome = (from u in db.Usuario where u.Login == perfil.ModificadoPor select u.Nome).FirstOrDefault();
            vm.ModificadoEm = perfil.ModificadoEm;

            return PartialView("_Edit", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(PerfilViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.FilialDDL = Util.GetEmpresas(vm.FilialId);
                return PartialView("_Edit", vm);
            }

            PerfilUsuario perfil = db.PerfilUsuario.Find(vm.Id);
            if (perfil == null)
            {
                return HttpNotFound();
            }

            if (db.PerfilUsuario.Any(p => p.Nome.ToLower() == vm.Nome.ToLower() && p.Id != vm.Id))
            {
                ModelState.AddModelError("Nome", "Já existe um perfil com este nome");
                vm.FilialDDL = Util.GetEmpresas(vm.FilialId);
                return PartialView("_Edit", vm);
            }

            perfil.Nome = vm.Nome;
            perfil.Descricao = vm.Descricao;
            perfil.FilialId = vm.FilialId;
            perfil.ModificadoPor = Util.GetCurrentUser();
            perfil.ModificadoEm = Util.GetCurrentDateTime();

            db.Entry(perfil).State = EntityState.Modified;

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

        [HttpPost]
        public ActionResult Delete(int id)
        {
            PerfilUsuario perfil = db.PerfilUsuario.Find(id);
            if (perfil == null)
            {
                return Json(new { success = false, msg = "Perfil não encontrado!" });
            }

            if (perfil.Id == 1)
            {
                return Json(new { success = false, msg = "O perfil Administrador não pode ser excluído!" });
            }

            bool temUsuario = db.Usuario.Any(u => u.PerfilId == id);
            if (temUsuario)
            {
                return Json(new { success = false, msg = "Não é possível excluir: existem usuários vinculados a este perfil." });
            }

            using (DbContextTransaction tr = db.Database.BeginTransaction())
            {
                try
                {
                    Util.DeleteAllowedAreasByPerfil(id, db);

                    var perfisFuncoes = db.PerfilFuncao.Where(pf => pf.IdPerfil == id).ToList();
                    foreach (var pf in perfisFuncoes)
                    {
                        db.PerfilFuncao.Remove(pf);
                    }
                    db.PerfilUsuario.Remove(perfil);
                    db.SaveChanges();
                    tr.Commit();
                }
                catch (DbEntityValidationException ex)
                {
                    string msgErro = "";
                    foreach (DbEntityValidationResult item in ex.EntityValidationErrors)
                    {
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
                    tr.Rollback();
                    return Json(new { success = false, msg = ex.Message });
                }
            }

            return Json(new { success = true, msg = "Operação realizada com sucesso" });
        }

        public ActionResult Detail(int id)
        {
            var vm = (from p in db.PerfilUsuario
                      where p.Id == id
                      select new PerfilViewModel
                      {
                          Id = p.Id,
                          Nome = p.Nome,
                          Descricao = p.Descricao,
                          FilialId = p.FilialId,
                          NomeFilial = (from e in db.Empresa where e.Id == p.FilialId select e.Nome).FirstOrDefault(),
                          CriadoEm = p.CriadoEm,
                          CriadoPor = p.CriadoPor,
                          CriadoPorNome = (from u in db.Usuario where u.Login == p.CriadoPor select u.Nome).FirstOrDefault(),
                          ModificadoEm = p.ModificadoEm,
                          ModificadoPor = p.ModificadoPor,
                          ModificadoPorNome = (from u in db.Usuario where u.Login == p.ModificadoPor select u.Nome).FirstOrDefault()
                      }).FirstOrDefault();

            if (vm == null)
            {
                return HttpNotFound();
            }

            return PartialView("_Detail", vm);
        }

        public ActionResult Funcoes(int id)
        {
            PerfilUsuario perfil = db.PerfilUsuario.Find(id);
            if (perfil == null)
            {
                return HttpNotFound();
            }

            var idsSelecionados = (from pf in db.PerfilFuncao
                                   where pf.IdPerfil == id && pf.Status == true
                                   select pf.IdFuncao).ToHashSet();

            var vm = new PerfilFuncoesViewModel
            {
                PerfilId = id,
                PerfilNome = perfil.Nome,
                Funcoes = (from f in db.AppFuncao
                           where f.Status == true
                           orderby f.CodComponente, f.Codigo
                           select new FuncaoPerfilItem
                           {
                               FuncaoId = f.Id,
                               Codigo = f.Codigo,
                               DescPTBR = f.DescPTBR,
                               CodComponente = f.CodComponente ?? string.Empty,
                               Controller = f.Controller ?? string.Empty,
                               Action = f.Action ?? string.Empty,
                               TituloMenu = (from m in db.AppMenu where m.Id == f.IdMenu select m.Titulo).FirstOrDefault() ?? string.Empty,
                               Selecionada = idsSelecionados.Contains(f.Id)
                           }).ToList()
            };

            return PartialView("Funcoes", vm);
        }

        public ActionResult Areas(int? id)
        {
            if (!id.HasValue)
            {
                return RedirectToAction("Index");
            }

            PerfilUsuario perfil = db.PerfilUsuario.Find(id.Value);
            if (perfil == null)
            {
                return HttpNotFound();
            }

            var areasSelecionadas = Util.GetAllowedAreasByPerfil(id.Value, db);

            var vm = new PerfilAreasViewModel
            {
                PerfilId = id.Value,
                PerfilNome = string.IsNullOrWhiteSpace(perfil.Descricao) ? perfil.Nome : perfil.Descricao,
                Areas = db.AppMenu
                    .Where(m => m.Status == true && m.Area != null && m.Area != string.Empty)
                    .Select(m => new
                    {
                        m.Area,
                        m.Titulo,
                        m.IdNivelSup,
                        m.Sequencia,
                        m.Id
                    })
                    .ToList()
                    .Where(m => !Util.IsIgnoredPerfilArea(m.Area))
                    .GroupBy(m => m.Area)
                    .Select(g => new AreaPerfilItem
                    {
                        Area = g.Key,
                        Titulo = g.Where(m => m.IdNivelSup == null)
                            .OrderBy(m => m.Sequencia)
                            .ThenBy(m => m.Id)
                            .Select(m => m.Titulo)
                            .FirstOrDefault()
                            ?? g.OrderBy(m => m.Sequencia).ThenBy(m => m.Id).Select(m => m.Titulo).FirstOrDefault(),
                        QuantidadeMenus = g.Count(),
                        Selecionada = areasSelecionadas.Contains(g.Key)
                    })
                    .OrderBy(x => x.Titulo)
                    .ThenBy(x => x.Area)
                    .ToList()
            };

            return PartialView("Areas", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Areas(PerfilAreasViewModel vm, string[] areasSelecionadas)
        {
            PerfilUsuario perfil = db.PerfilUsuario.Find(vm.PerfilId);
            if (perfil == null)
            {
                return HttpNotFound();
            }

            using (DbContextTransaction tr = db.Database.BeginTransaction())
            {
                try
                {
                    Util.SaveAllowedAreasByPerfil(vm.PerfilId, areasSelecionadas ?? new string[0], db);
                    db.SaveChanges();
                    tr.Commit();
                    Util.InvalidateMenuCache(vm.PerfilId);
                }
                catch (Exception ex)
                {
                    tr.Rollback();
                    return Json(new { success = false, msg = ex.Message });
                }
            }

            return Json(new { success = true, msg = "Áreas atualizadas com sucesso" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Funcoes(PerfilFuncoesViewModel vm, int[] funcoesSelecionadas)
        {
            PerfilUsuario perfil = db.PerfilUsuario.Find(vm.PerfilId);
            if (perfil == null)
            {
                return HttpNotFound();
            }

            var idsAntigos = (from pf in db.PerfilFuncao
                              where pf.IdPerfil == vm.PerfilId
                              select pf.IdFuncao).ToHashSet();

            var idsNovos = (funcoesSelecionadas ?? new int[0]).ToHashSet();

            var paraRemover = idsAntigos.Except(idsNovos).ToList();
            var paraAdicionar = idsNovos.Except(idsAntigos).ToList();

            using (DbContextTransaction tr = db.Database.BeginTransaction())
            {
                try
                {
                    foreach (var idFuncao in paraRemover)
                    {
                        var pf = db.PerfilFuncao.FirstOrDefault(x => x.IdPerfil == vm.PerfilId && x.IdFuncao == idFuncao);
                        if (pf != null)
                        {
                            db.PerfilFuncao.Remove(pf);
                        }
                    }

                    foreach (var idFuncao in paraAdicionar)
                    {
                        db.PerfilFuncao.Add(new PerfilFuncao
                        {
                            IdPerfil = vm.PerfilId,
                            IdFuncao = idFuncao,
                            Status = true,
                            CriadoPor = Util.GetCurrentUser(),
                            CriadoEm = Util.GetCurrentDateTime()
                        });
                    }

                    db.SaveChanges();
                    tr.Commit();
                    Util.InvalidateMenuCache(vm.PerfilId);
                }
                catch (Exception ex)
                {
                    tr.Rollback();
                    return Json(new { success = false, msg = ex.Message });
                }
            }

            return Json(new { success = true, msg = "Permissões atualizadas com sucesso" });
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
