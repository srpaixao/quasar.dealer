using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using System.Web;
using System.IO;
using System.Data;
using System.Data.SqlClient;

using Simplify.Quasar.Models;
using Simplify.Quasar.Areas.RecebimentoApp.ViewModels;
using Simplify.Quasar.Custom;
using WebHelpers.Mvc5.JqGrid;


namespace Simplify.Quasar.Areas.RecebimentoApp.Controllers
{
    [ValidateSession]
    public class VolumeController : Controller
    {
        Quasar_Entities db = new Quasar_Entities();

        int filialId = Util.GetCurrentFilial();

        public ActionResult Armazenar()
        {
            return View();
        }

        public ActionResult GetConferirItem()
        {
            var itens = (from nf in db.NotaFiscalItem
                         where nf.StatusId == 4 && nf.FilialId == filialId
                         select new VolumeViewModel
                         {
                             NFId = nf.Id,
                             ItemNr = nf.Item,
                             Quantidade = nf.Quantidade,
                             VolumeNr = nf.Volume,
                             Descricao = (from s in db.Material where s.Codigo == nf.Item select s.Descricao).FirstOrDefault(),
                         }).ToList();

            JsonResult result = Json(new { data = itens }, JsonRequestBehavior.AllowGet);
            result.MaxJsonLength = int.MaxValue;
            return result;
        }
        public ActionResult GetArmazenarItem()
        {
            var itens = (from nf in db.NotaFiscalItem
                         where nf.StatusId == 6 && nf.FilialId == filialId
                         select new VolumeViewModel
                         {
                             NFId = nf.Id,
                             ItemNr = nf.Item,
                             Quantidade = nf.Quantidade,
                             VolumeNr = nf.Volume,
                             Descricao = (from s in db.Material where s.Codigo == nf.Item select s.Descricao).FirstOrDefault(),
                             Locacao = (from i in db.Estoque where i.ItemNr == nf.Item select i.Locacao).FirstOrDefault(),
                         }).ToList();

            JsonResult result = Json(new { data = itens }, JsonRequestBehavior.AllowGet);
            result.MaxJsonLength = int.MaxValue;
            return result;
        }


        // GE: Pesquisa Item Nr
        [HttpPost]
        public ActionResult PesquisaItemNr(string itemnr)
        {

            var vm = (from item in db.Estoque
                      where item.ItemNr == itemnr && item.FilialId == filialId
                      select new VolumeViewModel
                      {
                          Locacao = item.Locacao,
                          Descricao = (from s in db.Material where s.Codigo == item.ItemNr select s.Descricao).FirstOrDefault(),

                      }).FirstOrDefault();

            if (vm == null)
            {
                return Json(new { retorno = vm, success = false, message = "Item Nr não cadastrado!" });
            }

            return Json(new { retorno = vm, success = true, message = "Item Nr cadastrado!" });

        }

        



    }

    






}