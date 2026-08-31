using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using Simplify.Quasar.Custom;
using Simplify.Quasar.ViewModels;

namespace Simplify.Quasar.Controllers
{
    [RequireUserSession]
    public class ManualController : Controller
    {
        private static readonly IList<ManualNavigationItemViewModel> Paginas =
            new List<ManualNavigationItemViewModel>
            {
                new ManualNavigationItemViewModel { Slug = "inicio", Titulo = "Visão geral", Icone = "fa-home" },
                new ManualNavigationItemViewModel { Slug = "recebimento", Titulo = "Recebimento", Icone = "fa-truck" },
                new ManualNavigationItemViewModel { Slug = "estoque", Titulo = "Estoque", Icone = "fa-cubes" },
                new ManualNavigationItemViewModel { Slug = "separacao", Titulo = "Separação", Icone = "fa-tasks" },
                new ManualNavigationItemViewModel { Slug = "expedicao", Titulo = "Expedição", Icone = "fa-truck" },
                new ManualNavigationItemViewModel { Slug = "devolucao", Titulo = "Devolução", Icone = "fa-undo" },
                new ManualNavigationItemViewModel { Slug = "anomalias", Titulo = "Anomalias", Icone = "fa-exclamation-triangle" },
                new ManualNavigationItemViewModel { Slug = "cadastros", Titulo = "Cadastros", Icone = "fa-cogs" }
            };

        [HttpGet]
        public ActionResult Index(string pagina, string q)
        {
            string slug = NormalizarPagina(pagina);
            ManualNavigationItemViewModel paginaAtual = Paginas.First(x => x.Slug == slug);
            string conteudo = CarregarConteudo(slug);

            ManualViewModel vm = new ManualViewModel
            {
                PaginaAtual = slug,
                Titulo = paginaAtual.Titulo,
                ConteudoHtml = ResolverLinks(conteudo),
                Pesquisa = (q ?? string.Empty).Trim(),
                Paginas = Paginas,
                Resultados = new List<ManualSearchResultViewModel>()
            };

            if (!string.IsNullOrWhiteSpace(vm.Pesquisa))
            {
                vm.Resultados = Pesquisar(vm.Pesquisa);
            }

            return View(vm);
        }

        [HttpGet]
        public ActionResult Asset(string path)
        {
            string relativePath = (path ?? string.Empty)
                .Replace('/', Path.DirectorySeparatorChar)
                .TrimStart(Path.DirectorySeparatorChar);
            string root = Path.GetFullPath(Server.MapPath("~/App_Data/Manual/assets"));
            string fullPath = Path.GetFullPath(Path.Combine(root, relativePath));

            if (!fullPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                !System.IO.File.Exists(fullPath))
            {
                return HttpNotFound();
            }

            string extension = Path.GetExtension(fullPath).ToLowerInvariant();
            string contentType;
            switch (extension)
            {
                case ".png":
                    contentType = "image/png";
                    break;
                case ".jpg":
                case ".jpeg":
                    contentType = "image/jpeg";
                    break;
                case ".gif":
                    contentType = "image/gif";
                    break;
                default:
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            Response.Cache.SetCacheability(HttpCacheability.Private);
            Response.Cache.SetMaxAge(TimeSpan.FromHours(8));
            return File(fullPath, contentType);
        }

        private string NormalizarPagina(string pagina)
        {
            string slug = string.IsNullOrWhiteSpace(pagina)
                ? "inicio"
                : pagina.Trim().ToLowerInvariant();
            return Paginas.Any(x => x.Slug == slug) ? slug : "inicio";
        }

        private string CarregarConteudo(string slug)
        {
            string path = Server.MapPath("~/App_Data/Manual/pages/" + slug + ".html");
            if (!System.IO.File.Exists(path))
            {
                return "<div class=\"alert alert-warning\">O conteúdo desta seção ainda não foi publicado.</div>";
            }

            return System.IO.File.ReadAllText(path, Encoding.UTF8);
        }

        private string ResolverLinks(string html)
        {
            string result = Regex.Replace(
                html ?? string.Empty,
                "__MANUAL_PAGE__([a-z0-9-]+)",
                match => Url.Action("Index", "Manual", new { pagina = match.Groups[1].Value }));

            return Regex.Replace(
                result,
                "__MANUAL_ASSET__([^\"']+)",
                match => Url.Action("Asset", "Manual", new { path = match.Groups[1].Value }));
        }

        private IList<ManualSearchResultViewModel> Pesquisar(string pesquisa)
        {
            string termo = NormalizarTexto(pesquisa);
            List<ManualSearchResultViewModel> resultados = new List<ManualSearchResultViewModel>();

            foreach (ManualNavigationItemViewModel pagina in Paginas)
            {
                string texto = ExtrairTexto(CarregarConteudo(pagina.Slug));
                string textoNormalizado = NormalizarTexto(texto);
                int index = textoNormalizado.IndexOf(termo, StringComparison.Ordinal);
                if (index < 0)
                {
                    continue;
                }

                int inicio = Math.Max(0, index - 90);
                int tamanho = Math.Min(240, texto.Length - inicio);
                string trecho = texto.Substring(inicio, tamanho).Trim();

                resultados.Add(new ManualSearchResultViewModel
                {
                    Slug = pagina.Slug,
                    Titulo = pagina.Titulo,
                    Trecho = (inicio > 0 ? "… " : string.Empty) +
                             trecho +
                             (inicio + tamanho < texto.Length ? " …" : string.Empty)
                });
            }

            return resultados;
        }

        private static string ExtrairTexto(string html)
        {
            string semTags = Regex.Replace(html ?? string.Empty, "<[^>]+>", " ");
            string decodificado = HttpUtility.HtmlDecode(semTags);
            return Regex.Replace(decodificado, "\\s+", " ").Trim();
        }

        private static string NormalizarTexto(string value)
        {
            return Util.RemoverAcentuacao(value ?? string.Empty).ToLowerInvariant();
        }
    }
}
