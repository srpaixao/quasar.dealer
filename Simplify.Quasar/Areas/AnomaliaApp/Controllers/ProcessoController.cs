using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Data;

using Simplify.Quasar.Models;
using Simplify.Quasar.Areas.AnomaliaApp.ViewModels;
using Simplify.Quasar.Custom;

namespace Simplify.Quasar.Areas.AnomaliaApp.Controllers
{
    [ValidateSession]
    public class ProcessoController : Controller
    {
        Quasar_Entities db = new Quasar_Entities();

        // GET: Admin/Home
        public ActionResult Index()
        {
            var vm = (from a in db.Anomalia
                      join s in db.StatusAnomalia on a.StatusId equals s.Id
                      select new AnomaliaViewModel
                      {
                          Id = a.Id,
                          StatusId = a.StatusId,
                          StatusDescricao = s.Descricao,
                          Controle = a.Controle,
                          FornecedorId = a.FornecedorId,
                          FornecedorNome = (from f in db.Fornecedor
                                            where f.Id == a.FornecedorId
                                            select f.Nome).FirstOrDefault(),
                          Observacoes = a.Observacoes,
                          CriadoPor = a.CriadoPor,
                          CriadoEm = a.CriadoEm,
                          ModificadoPor = a.ModificadoPor,
                          ModificadoEm = a.ModificadoEm
                      }).ToList();

            ViewBag.Permissoes = Util.GetPermissoes(ControllerContext.RouteData.Values["controller"].ToString());

            return View(vm);
        }

        public ActionResult Create()
        {
            AnomaliaViewModel vm = new AnomaliaViewModel();

            vm._itensAnomalia = new List<AnomaliaItemViewModel>();
            for (int i = 0; i < 5; i++)
            {
                AnomaliaItemViewModel item = new AnomaliaItemViewModel();
                item.Sequencial = i;
                vm._itensAnomalia.Add(item);
            }

            vm._itensDanificado = new List<AnomaliaItemViewModel>();
            for (int i = 0; i < 10; i++)
            {
                AnomaliaItemViewModel item = new AnomaliaItemViewModel();
                item.Sequencial = i;
                vm._itensDanificado.Add(item);
            }

            return View(vm);
        }

        public ActionResult GetNotaFiscal(string nota, string volume, string item)
        {
            var itens = (from nf in db.NotaFiscal
                         join i in db.NotaFiscalItem on nf.Id equals i.NotaFiscalId
                         where (nota == string.Empty || nf.Numero == nota) &&
                         (volume == string.Empty || i.Volume == volume) &&
                         (item == string.Empty || i.Item == item) 
                         orderby nf.Numero, i.Volume, i.Item
                         select new 
                           {
                             NotaFiscalId = i.Id,
                             NotaFiscal = nf.Numero,
                             DataEmissao = nf.DataEmissao,
                             Origem = (from o in db.OrigemNotaFiscal 
                                       where o.Codigo == nf.Observacoes
                                       select o.Descricao).FirstOrDefault() ?? string.Empty,
                             NumeroItem = i.Item,
                             DescricaoItem = (from m in db.Material
                                              where m.Codigo == i.Item
                                              select m.Descricao).FirstOrDefault() ?? string.Empty,
                             NumeroVolume = i.Volume,
                             Qtd = i.Quantidade,
                             NumeroPedido = i.Pedido
                           }).ToList();

            JsonResult result = Json(new { data = itens, success = true}, JsonRequestBehavior.AllowGet);
            result.MaxJsonLength = int.MaxValue;
            return result;
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