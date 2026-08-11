using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using Simplify.Quasar.Custom;
using Simplify.Quasar.Models;
using Simplify.Quasar.ViewModels;

namespace Simplify.Quasar.Controllers
{
    public class AccountController : Controller
    {
        Quasar_Entities db = new Quasar_Entities();

        int filialId = Util.GetCurrentFilial();

        // GET: Account
        public ActionResult Login()
        {
            ApplyNoCacheHeaders();

            LoginViewModel vm = new LoginViewModel();
            vm.SenhaExpirada = false;
            vm.Id = null;
            PrepareLoginView(vm);

            return View(vm);
        }

        // GET: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel vm)
        {
            ApplyNoCacheHeaders();

            if (string.IsNullOrWhiteSpace(vm.Usuario))
            {
                ModelState.AddModelError("Usuario", "Informe o usuário");
            }

            if (string.IsNullOrWhiteSpace(vm.Senha))
            {
                ModelState.AddModelError("Senha", "Informe a senha");
            }

            if (!ModelState.IsValid)
            {
                PrepareLoginView(vm);
                return View(vm);
            }

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

                            OnlineUserTracker.Track(
                                Session.SessionID,
                                user.Id,
                                user.FilialId ?? 0,
                                Session.Timeout);

                            return RedirectToAction("Index", "Home");
                        }
                    }
                }
            }

            PrepareLoginView(vm);
            return View(vm);
        }

        // GET: Account/AtualizarSenha
        public ActionResult AtualizarSenha(int id)
        {
            ApplyNoCacheHeaders();

            NewPasswordViewModel vm = new NewPasswordViewModel();
            vm.UsuarioId = id;
            return PartialView("_NovaSenha", vm);
        }

        // POST: Account/AtualizarSenha
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AtualizarSenha(NewPasswordViewModel vm)
        {
            ApplyNoCacheHeaders();

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
            OnlineUserTracker.Unregister(Session.SessionID);
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Login");
        }

        private void PrepareLoginView(LoginViewModel vm)
        {
            vm.Usuario = string.Empty;
            vm.Senha = string.Empty;
            ViewBag.ApplicationVersion = typeof(AccountController).Assembly.GetName().Version.ToString();

            ResetLoginFieldState("Usuario");
            ResetLoginFieldState("Senha");
        }

        private void ResetLoginFieldState(string fieldName)
        {
            if (!ModelState.ContainsKey(fieldName))
            {
                return;
            }

            List<string> errors = ModelState[fieldName]
                .Errors
                .Select(item => item.ErrorMessage)
                .Where(message => !string.IsNullOrWhiteSpace(message))
                .ToList();

            ModelState.Remove(fieldName);

            foreach (string error in errors)
            {
                ModelState.AddModelError(fieldName, error);
            }
        }

        private void ApplyNoCacheHeaders()
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            Response.Cache.SetExpires(DateTime.UtcNow.AddMinutes(-1));
            Response.Cache.SetMaxAge(TimeSpan.Zero);
            Response.Cache.SetRevalidation(HttpCacheRevalidation.AllCaches);
            Response.Cache.AppendCacheExtension("must-revalidate, proxy-revalidate");
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";
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
