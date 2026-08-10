using System;
using System.Linq;
using System.Web.Mvc;

using Simplify.Quasar.Custom;
using Simplify.Quasar.Models;
using Simplify.Quasar.ViewModels;
using System.Data.Entity;

namespace Simplify.Quasar.Controllers
{
    public class AccountController : Controller
    {
        Quasar_Entities db = new Quasar_Entities();

        // GET: Account
        public ActionResult Login()
        {
            LoginViewModel vm = new LoginViewModel();
            vm.SenhaExpirada = false;
            vm.Id = null;

            return View(vm);
        }

        // GET: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel vm)
        {
            Usuario user = (from u in db.Usuario
                            where u.Login.ToUpper() == vm.Usuario.ToUpper() && u.FilialId == vm.IdFilial
                            select u).FirstOrDefault();

            if (user == null)
            {
                ModelState.AddModelError("Usuario", "Usuário não cadastrado");
            }
            else
            {
                vm.Id = user.Id;

                if (!Util.ValidatePassword(vm.Senha, user.Senha))
                {
                    ModelState.AddModelError("Senha", "Senha incorreta");
                }
                else
                {
                    if (user.SenhaExpirada)
                    {
                        ModelState.Clear();
                        vm.SenhaExpirada = true;
                    }
                    else
                    {
                        vm.SenhaExpirada = false;
                        if (user.AcessoBloqueado == true)
                        {
                            ModelState.AddModelError("Usuario", "Acesso bloqueado");
                        }
                        else
                        {
                            try
                            {
                                user.UltimoAcesso = Util.GetCurrentDateTime();
                                db.Entry(user).State = EntityState.Modified;
                                db.SaveChanges();
                            }
                            catch (Exception)
                            {

                            }

                            Session["userid"] = user.Id;
                            Session["useraccount"] = user.Login;
                            Session["displayname"] = user.Nome;
                            Session["filialid"] = user.FilialId;
                            Session["perfilid"] = user.PerfilId;
                            Session["filialnome"] = (from f in db.Empresa
                                                     where f.Id == user.FilialId
                                                     select f.Nome).FirstOrDefault();

                            return RedirectToAction("Index", "Home");
                        }
                    }
                }
            }

            return View(vm);
        }

        // GET: Account/AtualizarSenha
        public ActionResult AtualizarSenha(int id)
        {
            NewPasswordViewModel vm = new NewPasswordViewModel();
            vm.UsuarioId = id;
            return PartialView("_NovaSenha", vm);
        }

        // POST: Account/AtualizarSenha
        [HttpPost]
        public ActionResult AtualizarSenha(NewPasswordViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return PartialView("_NovaSenha", vm);
            }

            var usuario = db.Usuario.Find(vm.UsuarioId);
            if (usuario == null)
            {
                return Json(new { success = false, message = "O usuário não foi localizado no Banco de Dados!" });
            }

            using (DbContextTransaction tr = db.Database.BeginTransaction())
            {
                try
                {
                    usuario.Senha = Util.HashPassword(vm.NovaSenha);
                    usuario.SenhaExpirada = false;
                    usuario.PerfilId = Util.GetPerfilId();
                    usuario.ModificadoEm = Util.GetCurrentDateTime();
                    db.Entry(usuario).State = EntityState.Modified;
                    db.SaveChanges();

                    tr.Commit();

                    return Json(new { success = true, message = "Senha atualizada com sucesso" });
                }
                catch (Exception ex)
                {
                    tr.Rollback();
                    return Json(new { success = false, message = ex.Message });
                }

            }
        }

        // GET: Account/Logout
        public ActionResult Logout()
        {
            Session["userid"] = null;
            Session["displayname"] = null;
            return RedirectToAction("Login");
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