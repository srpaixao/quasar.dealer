using System.IO;
using System.Web.Mvc;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Shapes;
using MigraDoc.DocumentObjectModel.Shapes.Charts;
using MigraDoc.Rendering;

using Simplify.Quasar.Custom;

namespace Simplify.Quasar.Controllers
{
    [ValidateSession]
    public class ReportController : Controller
    {
        int filialId = Util.GetCurrentFilial();

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult RomaneioEntrega()
        {
            var doc = new Document();
            doc.Info.Title = "Romaneio de Entrega";

            var section = doc.AddSection();
            section.PageSetup.Orientation = Orientation.Landscape;
            section.PageSetup.TopMargin = "2.5cm";
            section.PageSetup.BottomMargin = "1.8cm";
            section.PageSetup.LeftMargin = "1cm";
            section.PageSetup.RightMargin = "1cm";

            string romaneioId = "ROM-2026-000123";

            // HEADER
            var headerTable = section.Headers.Primary.AddTable();
            headerTable.Borders.Width = 0;
            headerTable.AddColumn("5cm");
            headerTable.AddColumn("16cm");
            headerTable.AddColumn("6cm");

            var headerRow = headerTable.AddRow();

            var imagePath = Server.MapPath("~/Content/img/logo.png");
            if (System.IO.File.Exists(imagePath))
            {
                var logo = headerRow.Cells[0].AddImage(imagePath);
                logo.Width = "3cm";
                logo.LockAspectRatio = true;
            }

            var title = headerRow.Cells[1].AddParagraph("ROMANEIO DE ENTREGA DE MERCADORIA");
            title.Format.Font.Size = 16;
            title.Format.Font.Bold = true;
            title.Format.Alignment = ParagraphAlignment.Center;

            var idText = headerRow.Cells[2].AddParagraph("Romaneio Nº\n" + romaneioId);
            idText.Format.Font.Size = 10;
            idText.Format.Font.Bold = true;
            idText.Format.Alignment = ParagraphAlignment.Right;

            section.AddParagraph("\n");

            // DADOS GERAIS
            var info = section.AddParagraph();
            info.AddFormattedText("Motorista: ", TextFormat.Bold);
            info.AddText("João da Silva    ");
            info.AddFormattedText("Veículo: ", TextFormat.Bold);
            info.AddText("ABC-1234    ");
            info.AddFormattedText("Data Saída: ", TextFormat.Bold);
            info.AddText(System.DateTime.Now.ToString("dd/MM/yyyy HH:mm"));

            section.AddParagraph("\n");

            // TABELA
            var table = section.AddTable();
            table.Borders.Width = 0.5;
            table.Rows.LeftIndent = 0;

            table.AddColumn("1.2cm");  // Seq
            table.AddColumn("3cm");    // NF
            table.AddColumn("5cm");    // Cliente
            table.AddColumn("6cm");    // Endereço
            table.AddColumn("3cm");    // Cidade
            table.AddColumn("2.5cm");  // Volumes
            table.AddColumn("2.5cm");  // Peso
            table.AddColumn("3cm");    // Status

            var row = table.AddRow();
            row.Shading.Color = Colors.LightGray;
            row.Format.Font.Bold = true;
            row.Format.Alignment = ParagraphAlignment.Center;

            row.Cells[0].AddParagraph("Seq.");
            row.Cells[1].AddParagraph("Nota Fiscal");
            row.Cells[2].AddParagraph("Cliente");
            row.Cells[3].AddParagraph("Endereço");
            row.Cells[4].AddParagraph("Cidade");
            row.Cells[5].AddParagraph("Volumes");
            row.Cells[6].AddParagraph("Peso Kg");
            row.Cells[7].AddParagraph("Status");

            for (int i = 1; i <= 15; i++)
            {
                var r = table.AddRow();

                r.Cells[0].AddParagraph(i.ToString());
                r.Cells[1].AddParagraph("NF-" + (1000 + i));
                r.Cells[2].AddParagraph("Cliente Exemplo " + i);
                r.Cells[3].AddParagraph("Rua de Entrega, " + (100 + i));
                r.Cells[4].AddParagraph("São Paulo");
                r.Cells[5].AddParagraph((i + 2).ToString());
                r.Cells[6].AddParagraph((i * 12.5M).ToString("N2"));
                r.Cells[7].AddParagraph("Pendente");
            }

            section.AddParagraph("\n");

            var obs = section.AddParagraph();
            obs.AddFormattedText("Observações: ", TextFormat.Bold);
            obs.AddText("Romaneio gerado para exemplo de roteiro de entrega.");

            // RODAPÉ
            var footer = section.Footers.Primary.AddParagraph();
            footer.Format.Alignment = ParagraphAlignment.Center;
            footer.Format.Font.Size = 9;

            footer.AddText("Página ");
            footer.AddPageField();
            footer.AddText(" de ");
            footer.AddNumPagesField();
            footer.AddText(" - Gerado em ");
            footer.AddText(System.DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));

            var renderer = new PdfDocumentRenderer();
            renderer.Document = doc;
            renderer.RenderDocument();

            using (var stream = new MemoryStream())
            {
                renderer.PdfDocument.Save(stream, false);
                return File(stream.ToArray(), "application/pdf");
                //return File(stream.ToArray(), "application/pdf", "RomaneioEntrega.pdf"); // Para forçar download com nome específico
            }
        }

        public ActionResult Teste1()
        {
            var doc = new Document();
            var section = doc.AddSection();

            // Cabeçalho com logotipo
            var header = section.Headers.Primary;
            var imagePath = Server.MapPath("~/Content/img/Avatar.PNG");
            var logo = header.AddImage(imagePath);
            logo.Width = "3cm";
            logo.LockAspectRatio = true;
            logo.Left = ShapePosition.Left;

            header.AddParagraph("Relatório de Teste")
                .Format.Font.Size = 14;

            // Corpo
            section.AddParagraph("Este é um relatório gerado sem query.");
            section.AddParagraph("Linha 1: Texto de exemplo.");
            section.AddParagraph("Linha 2: Outro texto de exemplo.");

            // Rodapé com paginação
            section.Footers.Primary.AddParagraph("Página ").AddPageField();

            // Renderizar PDF
            var renderer = new PdfDocumentRenderer() { Document = doc };
            renderer.RenderDocument();

            using (var stream = new MemoryStream())
            {
                renderer.PdfDocument.Save(stream, false);
                return File(stream.ToArray(), "application/pdf");
            }
        }

        public ActionResult Teste2()
        {
            var doc = new Document();
            var section = doc.AddSection();

            // Definir orientação paisagem
            section.PageSetup.Orientation = Orientation.Landscape;

            // Cabeçalho
            var header = section.Headers.Primary.AddParagraph("Relatório em Paisagem");
            header.Format.Font.Size = 14;
            header.Format.Font.Bold = true;
            header.Format.Alignment = ParagraphAlignment.Center;

            // Criar tabela
            var table = section.AddTable();
            table.Borders.Width = 0.75;
            table.Rows.LeftIndent = 0;

            table.AddColumn("3cm");
            table.AddColumn("6cm");
            table.AddColumn("6cm");

            var row = table.AddRow();
            row.Shading.Color = Colors.LightGray;
            row.Cells[0].AddParagraph("ID").Format.Font.Bold = true;
            row.Cells[1].AddParagraph("Nome").Format.Font.Bold = true;
            row.Cells[2].AddParagraph("Email").Format.Font.Bold = true;

            // Linhas de exemplo
            for (int i = 1; i <= 5; i++)
            {
                var r = table.AddRow();
                r.Cells[0].AddParagraph(i.ToString());
                r.Cells[1].AddParagraph("Cliente " + i);
                r.Cells[2].AddParagraph("cliente" + i + "@teste.com");
            }

            // Rodapé
            var footer = section.Footers.Primary.AddParagraph();
            footer.AddText("Página ");
            footer.AddPageField();
            footer.AddText(" - Gerado em " + System.DateTime.Now.ToString("dd/MM/yyyy"));

            // Renderizar PDF
            var renderer = new PdfDocumentRenderer() { Document = doc };
            renderer.RenderDocument();

            using (var stream = new MemoryStream())
            {
                renderer.PdfDocument.Save(stream, false);
                return File(stream.ToArray(), "application/pdf");
            }
        }

        public ActionResult Teste3()
        {
            var doc = new Document();
            var section = doc.AddSection();

            // Orientação paisagem
            section.PageSetup.Orientation = Orientation.Landscape;

            // Cabeçalho
            var header = section.Headers.Primary.AddParagraph("Relatório Completo");
            header.Format.Font.Size = 14;
            header.Format.Font.Bold = true;
            header.Format.Alignment = ParagraphAlignment.Center;

            // Tabela de exemplo
            var table = section.AddTable();
            table.Borders.Width = 0.75;
            table.Rows.LeftIndent = 0;

            table.AddColumn("3cm");
            table.AddColumn("6cm");
            table.AddColumn("6cm");

            var row = table.AddRow();
            row.Shading.Color = Colors.LightGray;
            row.Cells[0].AddParagraph("ID").Format.Font.Bold = true;
            row.Cells[1].AddParagraph("Nome").Format.Font.Bold = true;
            row.Cells[2].AddParagraph("Email").Format.Font.Bold = true;

            for (int i = 1; i <= 5; i++)
            {
                var r = table.AddRow();
                r.Cells[0].AddParagraph(i.ToString());
                r.Cells[1].AddParagraph("Cliente " + i);
                r.Cells[2].AddParagraph("cliente" + i + "@teste.com");
            }

            section.AddParagraph("\n");

            // Gráfico de barras (Column2D)
            // Gráfico de barras
            var chartBar = new Chart();
            chartBar.Type = ChartType.Column2D;
            chartBar.Width = "10cm";
            chartBar.Height = "6cm";

            var seriesBar = chartBar.SeriesCollection.AddSeries();
            seriesBar.Add(10);
            seriesBar.Add(25);
            seriesBar.Add(15);
            seriesBar.Add(30);

            var xSeriesBar = chartBar.XValues.AddXSeries();
            xSeriesBar.Add("A");
            xSeriesBar.Add("B");
            xSeriesBar.Add("C");
            xSeriesBar.Add("D");

            chartBar.XAxis.Title.Caption = "Categorias";
            chartBar.YAxis.Title.Caption = "Valores";

            section.Add(chartBar);

            section.AddParagraph("\n");

            // Gráfico de pizza (Pie2D)
            // Gráfico de pizza
            var chartPie = new Chart();
            chartPie.Type = ChartType.Pie2D;
            chartPie.Width = "10cm";
            chartPie.Height = "6cm";

            var seriesPie = chartPie.SeriesCollection.AddSeries();
            seriesPie.Add(40);
            seriesPie.Add(30);
            seriesPie.Add(20);
            seriesPie.Add(10);

            var xSeriesPie = chartPie.XValues.AddXSeries();
            xSeriesPie.Add("Produto A");
            xSeriesPie.Add("Produto B");
            xSeriesPie.Add("Produto C");
            xSeriesPie.Add("Produto D");

            section.Add(chartPie);

            // Rodapé
            var footer = section.Footers.Primary.AddParagraph();
            footer.AddText("Página ");
            footer.AddPageField();
            footer.AddText(" - Gerado em " + System.DateTime.Now.ToString("dd/MM/yyyy"));

            // Renderizar PDF
            var renderer = new PdfDocumentRenderer() { Document = doc };
            renderer.RenderDocument();

            using (var stream = new MemoryStream())
            {
                renderer.PdfDocument.Save(stream, false);
                return File(stream.ToArray(), "application/pdf");
            }
        }
    }
}








