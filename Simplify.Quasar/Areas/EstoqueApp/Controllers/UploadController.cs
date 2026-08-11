using Newtonsoft.Json;
using Simplify.Quasar.Areas.EstoqueApp.ViewModels;
using Simplify.Quasar.Custom;
using Simplify.Quasar.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;

namespace Simplify.Quasar.Areas.EstoqueApp.Controllers
{
    public class UploadController : Controller
    {
        Quasar_Entities db = new Quasar_Entities();

        int filialId = Util.GetCurrentFilial();

        // GET: EstoqueApp/Upload
        public ActionResult Index()
        {
            ViewBag.UltimaAtualizacaoEstoque = db.Database.SqlQuery<DateTime?>(
                "SELECT MAX(Dtatual) FROM dbo.EstoqueUpload_APOLLO WHERE FilialId = @p0",
                filialId).FirstOrDefault();

            return View();
        }

        [HttpPost]
        public ActionResult UploadFileEstoque(UploadArquivo vm)
        {
            string sql = string.Empty;
            string msg = string.Empty;

            string dms = (from a in db.AppConfig
                          where a.Nome == "DMS" && a.FilialId == filialId
                          select a.Valor).FirstOrDefault();
            if (string.IsNullOrEmpty(dms))
            {
                msg = "DMS não está configurado";
                return Json(new { erro = true, mensagem = msg }, JsonRequestBehavior.AllowGet);
            }

            if (vm.Arquivo == null)
            {
                msg = "Arquivo não informado";
                return Json(new { erro = true, mensagem = msg }, JsonRequestBehavior.AllowGet);
            }

            HttpPostedFileBase arquivo = vm.Arquivo;
            if (arquivo == null)
            {
                msg = "[HttpPostedFileBase] Não foi possível importar o arquivo informado";
                return Json(new { erro = true, mensagem = msg }, JsonRequestBehavior.AllowGet);
            }

            // Limpar tabela temporária
            if (string.Equals(dms, "APOLLO", StringComparison.OrdinalIgnoreCase))
            {
                return UploadApolloStock(arquivo);
            }

            try
            {
                db.Database.ExecuteSqlCommand("DELETE [EstoqueUpload] FROm [EstoqueUpload] WHERE FilialId = " + filialId);
                db.SaveChanges();

            }
            catch (Exception ex)
            {
                msg = "[EstoqueUploadColumns] DELETE TABLE failed<br>" + ex.Message;
                return Json(new { erro = true, mensagem = msg }, JsonRequestBehavior.AllowGet);
            }

            // Importar arquivo para tabela temporária
            int rows = 0;
            try
            {
                StreamReader reader = new StreamReader(arquivo.InputStream);
                string line;

                var dt = new DataTable();
                dt.Columns.Add(new DataColumn());
                dt.Columns.Add(new DataColumn("FilialId"));

                var dbConn = new SqlConnection(db.Database.Connection.ConnectionString);

                while ((line = reader.ReadLine()) != null)
                {
                    dt.Rows.Add(line, filialId);
                }

                var bullCopy = new SqlBulkCopy(dbConn, SqlBulkCopyOptions.TableLock, null)
                {
                    DestinationTableName = "EstoqueUpload",
                    BatchSize = dt.Rows.Count
                };

                dbConn.Open();
                bullCopy.WriteToServer(dt);
                dbConn.Close();
                bullCopy.Close();

                rows = dt.Rows.Count;

            }
            catch (Exception ex)
            {
                msg = "[EstoqueUpload] SqlBulkCopy failed<br>" + ex.Message;
                return Json(new { erro = true, mensagem = msg }, JsonRequestBehavior.AllowGet);
            }

            if (dms == "SERCON")
            {
                // Gerar tabela temporária de estoque
                try
                {
                    db.Database.ExecuteSqlCommand("DELETE [EstoqueUpload_SERCON] FROM [EstoqueUpload_SERCON] WHERE FilialId = " + filialId);
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    msg = "[EstoqueUpload_SERCON] TRUNCATE TABLE failed<br>" + ex.Message;
                    return Json(new { erro = true, mensagem = msg }, JsonRequestBehavior.AllowGet);
                }

                

                sql = (from s in db.AppSQL where s.Nome == "INSERT_EstoqueUpload_SERCON" select s.Comando).FirstOrDefault();
                if (!string.IsNullOrEmpty(sql))
                {
                    sql = Util.FormatSQL(sql);
                    //sql = sql.Replace("@FilialId", filialId.ToString());

                    try
                    {
                        db.Database.ExecuteSqlCommand(sql);
                        db.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        msg = "[EstoqueUpload] INSERT failed<br>" + ex.Message;
                        return Json(new { erro = true, mensagem = msg }, JsonRequestBehavior.AllowGet);
                    }
                }

                // Aplicar filtro de locações
                try
                {
                    List<string> exclude_locacoes = new List<string>();
                    string config = (from c in db.AppConfig where c.Nome == "ExcludeLoc" select c.Valor).FirstOrDefault();
                    List<string> filtro = config.Split(';').ToList();
                    foreach (string item in filtro)
                    {
                        List<string> locacoes = (from l in db.EstoqueUpload_SERCON where l.Local.StartsWith(item) select l.Local).ToList();
                        if (locacoes != null)
                        {
                            exclude_locacoes.AddRange(locacoes);
                        }
                    }
                    db.EstoqueUpload_SERCON.RemoveRange(db.EstoqueUpload_SERCON.Where(x => exclude_locacoes.Contains(x.Local)));
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    msg = "[EstoqueUpload] DELETE failed<br>" + ex.Message;
                    return Json(new { erro = true, mensagem = msg }, JsonRequestBehavior.AllowGet);
                }

                // Atualizar tabela de estoque
                sql = (from s in db.AppSQL where s.Nome == "UPDATE_Estoque_From_SERCON" select s.Comando).FirstOrDefault();
                if (!string.IsNullOrEmpty(sql))
                {
                    sql = Util.FormatSQL(sql);
                    //sql = sql.Replace("@FilialId", filialId.ToString());

                    try
                    {
                        db.Database.ExecuteSqlCommand(sql);
                        db.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        msg = "[Estoque] MERGE failed<br>" + ex.Message;
                        return Json(new { erro = true, mensagem = msg }, JsonRequestBehavior.AllowGet);
                    }
                }

                // Atualizar tabela de materiais
                sql = (from s in db.AppSQL where s.Nome == "UPDATE_Material_From_SERCON" select s.Comando).FirstOrDefault();
                if (!string.IsNullOrEmpty(sql))
                {
                    sql = Util.FormatSQL(sql);
                    //sql = sql.Replace("@FilialId", filialId.ToString());

                    try
                    {
                        db.Database.ExecuteSqlCommand(sql);
                        db.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        msg = "[Material] MERGE failed<br>" + ex.Message;
                        return Json(new { erro = true, mensagem = msg }, JsonRequestBehavior.AllowGet);
                    }
                }

                // Atualizar tabela de locações
                sql = (from s in db.AppSQL where s.Nome == "UPDATE_Locacao_From_SERCON" select s.Comando).FirstOrDefault();
                if (!string.IsNullOrEmpty(sql))
                {
                    sql = Util.FormatSQL(sql);

                    try
                    {
                        db.Database.ExecuteSqlCommand(sql);
                        db.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        msg = "[Locacao] MERGE failed<br>" + ex.Message;
                        return Json(new { erro = true, mensagem = msg }, JsonRequestBehavior.AllowGet);
                    }
                }
            }
            else
            {
                if (dms == "APOLLO")
                {
                    // Gerar tabela temporária de estoque
                    //try
                    //{
                    //    db.Database.ExecuteSqlCommand("DELETE [EstoqueUpload] FROM [EstoqueUpload] WHERE FilialId = " + filialId);
                    //    db.SaveChanges();
                    //}
                    //catch (Exception ex)
                    //{
                    //    msg = "[EstoqueUpload] DELETE TABLE failed<br>" + ex.Message;
                    //    return Json(new { erro = true, mensagem = msg }, JsonRequestBehavior.AllowGet);
                    //}

                    sql = (from s in db.AppSQL where s.Nome == "INSERT_EstoqueUpload" select s.Comando).FirstOrDefault();
                    if (!string.IsNullOrEmpty(sql))
                    {
                        sql = Util.FormatSQL(sql);
                        
                        try
                        {
                            db.Database.ExecuteSqlCommand(sql);
                            db.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            msg = "[EstoqueUpload] INSERT failed<br>" + ex.Message;
                            return Json(new { erro = true, mensagem = msg }, JsonRequestBehavior.AllowGet);
                        }
                    }

                    // Aplicar filtro de locações
                    //try
                    //{
                    //    List<string> exclude_locacoes = new List<string>();
                    //    string config = (from c in db.AppConfig where c.Nome == "ExcludeLoc" select c.Valor).FirstOrDefault();
                    //    List<string> filtro = config.Split(';').ToList();
                    //    foreach (string item in filtro)
                    //    {
                    //        List<string> locacoes = (from l in db.EstoqueUpload_APOLLO where l.Local.StartsWith(item) select l.Local).ToList();
                    //        if (locacoes != null)
                    //        {
                    //            exclude_locacoes.AddRange(locacoes);
                    //        }
                    //    }
                    //    db.EstoqueUpload_APOLLO.RemoveRange(db.EstoqueUpload_APOLLO.Where(x => exclude_locacoes.Contains(x.Local)));
                    //    db.SaveChanges();
                    //}
                    //catch (Exception ex)
                    //{
                    //    msg = "[EstoqueUpload] DELETE failed<br>" + ex.Message;
                    //    return Json(new { erro = true, mensagem = msg }, JsonRequestBehavior.AllowGet);
                    //}

                    // Atualizar tabela de estoque
                    sql = (from s in db.AppSQL where s.Nome == "UPDATE_Estoque" select s.Comando).FirstOrDefault();
                    if (!string.IsNullOrEmpty(sql))
                    {
                        sql = Util.FormatSQL(sql);

                        try
                        {
                            db.Database.ExecuteSqlCommand(sql);
                            db.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            msg = "[Estoque] MERGE failed<br>" + ex.Message;
                            return Json(new { erro = true, mensagem = msg }, JsonRequestBehavior.AllowGet);
                        }
                    }

                    // Atualizar LOCAÇÕES na tabela de estoque onde qtde Estoque = ZERO
                    // Comando legado desativado: o upload APOLLO nunca deve excluir itens de Estoque.

                    // Atualizar tabela de materiais
                    sql = (from s in db.AppSQL where s.Nome == "UPDATE_Material_From_APOLLO" select s.Comando).FirstOrDefault();
                    if (!string.IsNullOrEmpty(sql))
                    {
                        sql = Util.FormatSQL(sql);
                        
                        try
                        {
                            db.Database.ExecuteSqlCommand(sql);
                            db.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            msg = "[Material] MERGE failed<br>" + ex.Message;
                            return Json(new { erro = true, mensagem = msg }, JsonRequestBehavior.AllowGet);
                        }
                    }

                    // Atualizar tabela de locações
                    sql = (from s in db.AppSQL where s.Nome == "UPDATE_Locacao_From_APOLLO" select s.Comando).FirstOrDefault();
                    if (!string.IsNullOrEmpty(sql))
                    {
                        sql = Util.FormatSQL(sql);
                        
                        try
                        {
                            db.Database.ExecuteSqlCommand(sql);
                            db.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            msg = "[Locacao] MERGE failed<br>" + ex.Message;
                            return Json(new { erro = true, mensagem = msg }, JsonRequestBehavior.AllowGet);
                        }
                    }
                }
                else
                {
                    msg = "DMS incorreto";
                    return Json(new { erro = true, mensagem = msg }, JsonRequestBehavior.AllowGet);
                }
            }

            msg = "Arquivo importado com sucesso";
            return Json(new { erro = false, mensagem = msg, qtd_linhas = rows }, JsonRequestBehavior.AllowGet);
        }

        private ActionResult UploadApolloStock(HttpPostedFileBase arquivo)
        {
            bool limparLocacaoSaldoZero = db.AppConfig
                .Where(x => x.Nome == "LimparLocacaoSaldoZero" && x.FilialId == filialId)
                .Select(x => x.Valor)
                .AsEnumerable()
                .Select(IsEnabledConfigValue)
                .FirstOrDefault();

            string excludeConfig = db.AppConfig
                .Where(x => x.Nome == "ExcludeLoc" && x.FilialId == filialId)
                .Select(x => x.Valor)
                .FirstOrDefault() ?? string.Empty;

            var excludedPrefixes = excludeConfig
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => x.Length > 0)
                .ToList();

            var rawData = CreateRawUploadTable();
            var stageData = CreateApolloStageTable();
            var materialItemApolloData = CreateMaterialItemApolloTable();
            var itemNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int excludedCount = 0;
            int lineNumber = 0;

            try
            {
                using (var reader = new StreamReader(arquivo.InputStream))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        lineNumber++;
                        if (string.IsNullOrWhiteSpace(line))
                        {
                            continue;
                        }

                        rawData.Rows.Add(line, filialId);

                        if (HasItemCodeWithEdgeWhitespace(line))
                        {
                            excludedCount++;
                            continue;
                        }

                        ApolloStockRow row = ParseApolloStockRow(line, lineNumber);
                        if (row == null)
                        {
                            continue;
                        }

                        if (!itemNumbers.Add(row.Item))
                        {
                            throw new InvalidDataException("Item duplicado no arquivo: " + row.Item + ".");
                        }

                        if (excludedPrefixes.Any(prefix => row.Location.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                        {
                            excludedCount++;
                            continue;
                        }

                        stageData.Rows.Add(
                            row.Item,
                            row.Description,
                            row.AccountingQuantity,
                            row.OrderedQuantity,
                            row.QuotedQuantity,
                            row.InTransitQuantity,
                            row.AvailableQuantity,
                            row.Location,
                            row.AverageCost,
                            row.AverageDemand,
                            Util.GetCurrentDateTime(),
                            filialId);

                        if (!string.IsNullOrWhiteSpace(row.ItemApollo))
                        {
                            materialItemApolloData.Rows.Add(row.Item, row.ItemApollo);
                        }
                    }
                }

                if (rawData.Rows.Count == 0)
                {
                    throw new InvalidDataException("O arquivo de estoque esta vazio.");
                }

                if (stageData.Rows.Count == 0)
                {
                    throw new InvalidDataException("Nenhum item valido foi encontrado para a filial.");
                }

                using (var connection = new SqlConnection(db.Database.Connection.ConnectionString))
                {
                    connection.Open();

                    using (var transaction = connection.BeginTransaction(IsolationLevel.Serializable))
                    {
                        try
                        {
                            AcquireStockUploadLock(connection, transaction);
                            DeleteBranchUploadData(connection, transaction);
                            BulkInsert(connection, transaction, "dbo.EstoqueUpload", rawData);
                            BulkInsert(connection, transaction, "dbo.EstoqueUpload_APOLLO", stageData);

                            ApolloMergeResult result = MergeApolloStock(
                                connection,
                                transaction,
                                limparLocacaoSaldoZero);
                            int materialItemApolloUpdated = UpdateMaterialItemApollo(
                                connection,
                                transaction,
                                materialItemApolloData,
                                Util.GetCurrentUser(),
                                Util.GetCurrentDateTime());
                            transaction.Commit();

                            return Json(new
                            {
                                erro = false,
                                mensagem = "Arquivo importado com sucesso.",
                                atualizado_em = Util.GetCurrentDateTime().ToString("dd/MM/yyyy HH:mm:ss"),
                                qtd_linhas = Math.Max(rawData.Rows.Count - 1, 0),
                                processados = stageData.Rows.Count,
                                desconsiderados = excludedCount,
                                inseridos = result.Inserted,
                                atualizados = result.Updated,
                                materiais_inseridos = result.MaterialsInserted,
                                materiais_atualizados = result.MaterialsUpdated,
                                materiais_item_apollo_atualizados = materialItemApolloUpdated,
                                locacoes_inseridas = result.LocationsInserted,
                                locacoes_atualizadas = 0
                            }, JsonRequestBehavior.AllowGet);
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    erro = true,
                    mensagem = "Nao foi possivel atualizar o estoque: " + ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        private ApolloStockRow ParseApolloStockRow(string line, int lineNumber)
        {
            var fields = (line ?? string.Empty).Split(';').ToList();
            if (fields.Count > 0 && fields[fields.Count - 1].Length == 0 && line.EndsWith(";", StringComparison.Ordinal))
            {
                fields.RemoveAt(fields.Count - 1);
            }

            if (fields.Count < 11)
            {
                throw new InvalidDataException("Linha " + lineNumber + " possui menos de 11 campos.");
            }

            string itemApollo = fields[0].Trim();
            string item = fields[1].Trim();
            if (itemApollo.Equals("ITEM_ESTOQUE", StringComparison.OrdinalIgnoreCase)
                || item.Equals("ITEM_ESTOQUE_PUB", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (item.Length == 0)
            {
                return null;
            }

            const int trailingFieldCount = 8;
            int dataStartIndex = fields.Count - trailingFieldCount;
            var descriptionParts = fields
                .Skip(2)
                .Take(dataStartIndex - 2)
                .Where(x => !x.Trim().StartsWith("NFCI:", StringComparison.OrdinalIgnoreCase));
            string description = Regex.Replace(
                string.Join(";", descriptionParts),
                "&apos;",
                "'",
                RegexOptions.IgnoreCase).Trim();

            return new ApolloStockRow
            {
                ItemApollo = itemApollo,
                Item = item,
                Description = description,
                AccountingQuantity = ParseApolloInteger(fields[dataStartIndex], lineNumber, "quantidade contabil"),
                OrderedQuantity = ParseApolloInteger(fields[dataStartIndex + 1], lineNumber, "quantidade pedida"),
                QuotedQuantity = ParseApolloInteger(fields[dataStartIndex + 2], lineNumber, "quantidade orcada"),
                InTransitQuantity = ParseApolloInteger(fields[dataStartIndex + 3], lineNumber, "quantidade em transito"),
                AvailableQuantity = ParseApolloInteger(fields[dataStartIndex + 4], lineNumber, "quantidade disponivel"),
                Location = NormalizeLocation(fields[dataStartIndex + 5]),
                AverageCost = ParseApolloDecimal(fields[dataStartIndex + 6], lineNumber, "custo medio"),
                AverageDemand = ParseApolloDecimal(fields[dataStartIndex + 7], lineNumber, "demanda media")
            };
        }

        private static bool HasItemCodeWithEdgeWhitespace(string line)
        {
            string source = line ?? string.Empty;
            int firstSeparatorIndex = source.IndexOf(';');
            if (firstSeparatorIndex < 0)
            {
                return false;
            }

            int secondSeparatorIndex = source.IndexOf(';', firstSeparatorIndex + 1);
            if (secondSeparatorIndex <= firstSeparatorIndex + 1)
            {
                return false;
            }

            string rawItem = source.Substring(
                firstSeparatorIndex + 1,
                secondSeparatorIndex - firstSeparatorIndex - 1);
            return rawItem.Length > 0
                && (char.IsWhiteSpace(rawItem[0]) || char.IsWhiteSpace(rawItem[rawItem.Length - 1]));
        }

        private static int ParseApolloInteger(string value, int lineNumber, string fieldName)
        {
            string normalized = (value ?? string.Empty).Trim();
            if (normalized.Length == 0)
            {
                return 0;
            }

            decimal decimalValue;
            if (!decimal.TryParse(
                normalized,
                NumberStyles.Number,
                CultureInfo.GetCultureInfo("pt-BR"),
                out decimalValue))
            {
                throw new InvalidDataException(
                    "Linha " + lineNumber + ": " + fieldName +
                    " invalida. Valor recebido: '" + normalized + "'.");
            }

            if (decimalValue != decimal.Truncate(decimalValue))
            {
                throw new InvalidDataException(
                    "Linha " + lineNumber + ": " + fieldName +
                    " possui quantidade fracionaria. Valor recebido: '" + normalized + "'.");
            }

            if (decimalValue < int.MinValue || decimalValue > int.MaxValue)
            {
                throw new InvalidDataException(
                    "Linha " + lineNumber + ": " + fieldName +
                    " excede o limite permitido. Valor recebido: '" + normalized + "'.");
            }

            return decimal.ToInt32(decimalValue);
        }

        private static decimal ParseApolloDecimal(string value, int lineNumber, string fieldName)
        {
            decimal result;
            string normalized = (value ?? string.Empty).Trim();
            if (normalized.Length == 0)
            {
                return 0m;
            }

            if (!decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.GetCultureInfo("pt-BR"), out result))
            {
                throw new InvalidDataException("Linha " + lineNumber + ": " + fieldName + " invalido.");
            }

            return result;
        }

        private static string NormalizeLocation(string value)
        {
            return string.Join(" ", (value ?? string.Empty)
                .Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries));
        }

        private static bool IsEnabledConfigValue(string value)
        {
            string normalized = Util.RemoverAcentuacao(value ?? string.Empty)
                .Trim()
                .ToUpperInvariant();

            return normalized == "TRUE" ||
                   normalized == "1" ||
                   normalized == "SIM" ||
                   normalized == "S";
        }

        private static DataTable CreateRawUploadTable()
        {
            var table = new DataTable();
            table.Columns.Add("Linha", typeof(string));
            table.Columns.Add("FilialId", typeof(int));
            return table;
        }

        private static DataTable CreateApolloStageTable()
        {
            var table = new DataTable();
            table.Columns.Add("Item", typeof(string));
            table.Columns.Add("Descricao", typeof(string));
            table.Columns.Add("QtdContabil", typeof(int));
            table.Columns.Add("QtdPedida", typeof(int));
            table.Columns.Add("QtdOrcada", typeof(int));
            table.Columns.Add("QtdTransito", typeof(int));
            table.Columns.Add("QtdDisponivel", typeof(int));
            table.Columns.Add("Local", typeof(string));
            table.Columns.Add("CustoMedio", typeof(decimal));
            table.Columns.Add("DemandaMedia", typeof(decimal));
            table.Columns.Add("Dtatual", typeof(DateTime));
            table.Columns.Add("FilialId", typeof(int));
            return table;
        }

        private static DataTable CreateMaterialItemApolloTable()
        {
            var table = new DataTable();
            table.Columns.Add("Codigo", typeof(string));
            table.Columns.Add("ItemApollo", typeof(string));
            return table;
        }

        private void AcquireStockUploadLock(SqlConnection connection, SqlTransaction transaction)
        {
            const string commandText = @"
DECLARE @Result INT;
EXEC @Result = sys.sp_getapplock
    @Resource = @Resource,
    @LockMode = 'Exclusive',
    @LockOwner = 'Transaction',
    @LockTimeout = 30000;
IF @Result < 0
    THROW 50001, 'Nao foi possivel bloquear a importacao de estoque desta filial.', 1;";

            using (var command = new SqlCommand(commandText, connection, transaction))
            {
                command.Parameters.Add("@Resource", SqlDbType.NVarChar, 255).Value =
                    "Quasar.EstoqueUpload." + filialId.ToString(CultureInfo.InvariantCulture);
                command.ExecuteNonQuery();
            }
        }

        private void DeleteBranchUploadData(SqlConnection connection, SqlTransaction transaction)
        {
            const string commandText = @"
DELETE FROM dbo.EstoqueUpload WHERE FilialId = @FilialId;
DELETE FROM dbo.EstoqueUpload_APOLLO WHERE FilialId = @FilialId;";

            using (var command = new SqlCommand(commandText, connection, transaction))
            {
                command.Parameters.Add("@FilialId", SqlDbType.Int).Value = filialId;
                command.ExecuteNonQuery();
            }
        }

        private static void BulkInsert(
            SqlConnection connection,
            SqlTransaction transaction,
            string destinationTable,
            DataTable data)
        {
            using (var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.TableLock, transaction))
            {
                bulkCopy.DestinationTableName = destinationTable;
                bulkCopy.BatchSize = Math.Min(data.Rows.Count, 5000);
                bulkCopy.BulkCopyTimeout = 300;

                foreach (DataColumn column in data.Columns)
                {
                    bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
                }

                bulkCopy.WriteToServer(data);
            }
        }

        private static int UpdateMaterialItemApollo(
            SqlConnection connection,
            SqlTransaction transaction,
            DataTable materialItemApolloData,
            string usuarioAtual,
            DateTime agora)
        {
            if (materialItemApolloData == null || materialItemApolloData.Rows.Count == 0)
            {
                return 0;
            }

            const string createTempTableCommand = @"
SELECT TOP (0) Codigo, ItemApollo
INTO #MaterialItemApollo
FROM dbo.Material;";

            using (var command = new SqlCommand(createTempTableCommand, connection, transaction))
            {
                command.ExecuteNonQuery();
            }

            BulkInsert(
                connection,
                transaction,
                "#MaterialItemApollo",
                materialItemApolloData);

            const string updateCommand = @"
UPDATE Destino
   SET Destino.ItemApollo = CASE
           WHEN NULLIF(LTRIM(RTRIM(Destino.ItemApollo)), '') IS NULL
               THEN NULLIF(LTRIM(RTRIM(Origem.ItemApollo)), '')
           ELSE Destino.ItemApollo
       END,
       Destino.CriadoEm = ISNULL(Destino.CriadoEm, @Agora),
       Destino.ModificadoPor = @Usuario,
       Destino.ModificadoEm = @Agora
  FROM dbo.Material Destino
  INNER JOIN #MaterialItemApollo Origem
          ON Origem.Codigo = Destino.Codigo
 WHERE NULLIF(LTRIM(RTRIM(Origem.ItemApollo)), '') IS NOT NULL;";

            using (var command = new SqlCommand(updateCommand, connection, transaction))
            {
                command.CommandTimeout = 300;
                command.Parameters.Add("@Usuario", SqlDbType.VarChar, 100).Value =
                    (object)usuarioAtual ?? DBNull.Value;
                command.Parameters.Add("@Agora", SqlDbType.DateTime).Value = agora;
                return command.ExecuteNonQuery();
            }
        }

        private ApolloMergeResult MergeApolloStock(
            SqlConnection connection,
            SqlTransaction transaction,
            bool limparLocacaoSaldoZero)
        {
            const string commandText = @"
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Actions TABLE (ActionName NVARCHAR(10) NOT NULL);
DECLARE @MaterialActions TABLE (ActionName NVARCHAR(10) NOT NULL);
DECLARE @LocationActions TABLE (ActionName NVARCHAR(10) NOT NULL);

IF EXISTS
(
    SELECT 1
    FROM dbo.EstoqueUpload_APOLLO Origem
    INNER JOIN dbo.Locacao Destino ON Destino.Codigo = Origem.Local
    WHERE Origem.FilialId = @FilialId
      AND NULLIF(Origem.Local, '') IS NOT NULL
      AND ISNULL(Destino.FilialId, -1) <> @FilialId
)
    THROW 50003, 'Existe locacao do arquivo cadastrada em outra filial.', 1;

MERGE dbo.Material WITH (HOLDLOCK) AS Destino
USING
(
    SELECT Item, Descricao
    FROM dbo.EstoqueUpload_APOLLO
    WHERE FilialId = @FilialId
) AS Origem
ON Destino.Codigo = Origem.Item
WHEN NOT MATCHED BY TARGET THEN
    INSERT
    (
        Codigo, Descricao, UN, EmbalagemMin, MediaVendas, CustoUnitario,
        Curva, ItemCritico, ObsItemCritico, CategoriaProduto,
        CriadoPor, CriadoEm, FilialId
    )
    VALUES
    (
        Origem.Item, Origem.Descricao, '', NULL, NULL,
        NULL, 'N', 0, NULL, 'Diretos',
        @Usuario, @Agora, @FilialId
    )
OUTPUT $action INTO @MaterialActions(ActionName);

MERGE dbo.Locacao WITH (HOLDLOCK) AS Destino
USING
(
    SELECT DISTINCT Local AS Codigo
    FROM dbo.EstoqueUpload_APOLLO
    WHERE FilialId = @FilialId
      AND NULLIF(Local, '') IS NOT NULL
) AS Origem
ON Destino.Codigo = Origem.Codigo
AND Destino.FilialId = @FilialId
WHEN NOT MATCHED BY TARGET THEN
    INSERT
    (
        Codigo, Tipo, Descricao, Bloqueado, AreaId, EquipamentoId,
        Curva, Estrategia, Observacoes, CriadoPor, CriadoEm, FilialId, ZonaId
    )
    VALUES
    (
        Origem.Codigo, 'P', '', 0, NULL, NULL,
        NULL, NULL, NULL, @Usuario, @Agora, @FilialId, NULL
    )
OUTPUT $action INTO @LocationActions(ActionName);

MERGE dbo.Estoque WITH (HOLDLOCK) AS Destino
USING
(
    SELECT Item, Local, QtdDisponivel
    FROM dbo.EstoqueUpload_APOLLO
    WHERE FilialId = @FilialId
) AS Origem
ON Destino.ItemNr = Origem.Item
AND Destino.FilialId = @FilialId
WHEN MATCHED THEN
    UPDATE SET
        Destino.Locacao = CASE
            WHEN @LimparLocacaoSaldoZero = 1 AND Origem.QtdDisponivel = 0 THEN NULL
            ELSE Origem.Local
        END,
        Destino.Saldo = Origem.QtdDisponivel,
        Destino.ModificadoPor = @Usuario,
        Destino.ModificadoEm = @Agora
WHEN NOT MATCHED BY TARGET THEN
    INSERT
    (
        Locacao, ItemNr, Saldo, Indisponivel, PedidoPendente,
        ValorEstoque, Range, CriadoPor, CriadoEm, FilialId
    )
    VALUES
    (
        CASE
            WHEN @LimparLocacaoSaldoZero = 1 AND Origem.QtdDisponivel = 0 THEN NULL
            ELSE Origem.Local
        END,
        Origem.Item, Origem.QtdDisponivel, NULL, NULL,
        NULL, NULL, @Usuario, @Agora, @FilialId
    )
OUTPUT $action INTO @Actions(ActionName);

SELECT
    Inseridos = (SELECT COUNT(*) FROM @Actions WHERE ActionName = 'INSERT'),
    Atualizados = (SELECT COUNT(*) FROM @Actions WHERE ActionName = 'UPDATE'),
    MateriaisInseridos = (SELECT COUNT(*) FROM @MaterialActions WHERE ActionName = 'INSERT'),
    MateriaisAtualizados = (SELECT COUNT(*) FROM @MaterialActions WHERE ActionName = 'UPDATE'),
    LocacoesInseridas = (SELECT COUNT(*) FROM @LocationActions WHERE ActionName = 'INSERT');";

            using (var command = new SqlCommand(commandText, connection, transaction))
            {
                command.CommandTimeout = 300;
                command.Parameters.Add("@FilialId", SqlDbType.Int).Value = filialId;
                command.Parameters.Add("@LimparLocacaoSaldoZero", SqlDbType.Bit).Value =
                    limparLocacaoSaldoZero;
                command.Parameters.Add("@Usuario", SqlDbType.VarChar, 100).Value =
                    (object)Util.GetCurrentUser() ?? DBNull.Value;
                command.Parameters.Add("@Agora", SqlDbType.DateTime).Value =
                    Util.GetCurrentDateTime();

                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        throw new InvalidOperationException("O merge de estoque nao retornou resultado.");
                    }

                    return new ApolloMergeResult
                    {
                        Inserted = Convert.ToInt32(reader["Inseridos"]),
                        Updated = Convert.ToInt32(reader["Atualizados"]),
                        MaterialsInserted = Convert.ToInt32(reader["MateriaisInseridos"]),
                        MaterialsUpdated = Convert.ToInt32(reader["MateriaisAtualizados"]),
                        LocationsInserted = Convert.ToInt32(reader["LocacoesInseridas"])
                    };
                }
            }
        }

        private sealed class ApolloStockRow
        {
            public string ItemApollo { get; set; }
            public string Item { get; set; }
            public string Description { get; set; }
            public int AccountingQuantity { get; set; }
            public int OrderedQuantity { get; set; }
            public int QuotedQuantity { get; set; }
            public int InTransitQuantity { get; set; }
            public int AvailableQuantity { get; set; }
            public string Location { get; set; }
            public decimal AverageCost { get; set; }
            public decimal AverageDemand { get; set; }
        }

        private sealed class ApolloMergeResult
        {
            public int Inserted { get; set; }
            public int Updated { get; set; }
            public int MaterialsInserted { get; set; }
            public int MaterialsUpdated { get; set; }
            public int LocationsInserted { get; set; }
        }

        public ActionResult ConfigUpload()
        {
            ConfigUploadViewModel vm = new ConfigUploadViewModel();
            AppConfig config = db.AppConfig.Where(x => x.Nome == "ExcludeLoc").FirstOrDefault();
            if (config != null)
            {
                vm.Id = config.Id;
                vm.Nome = config.Nome;
                vm.Descricao = config.Descricao;
                vm.Valor = config.Valor;

                vm.ModificadoPor = config.ModificadoPor;
                vm.ModificadoPorNome = (from u in db.Usuario where u.FilialId == filialId && u.Login == config.ModificadoPor select u.Nome).FirstOrDefault();
                vm.ModificadoEm = config.ModificadoEm;

                if (config.ModificadoPor == null)
                {
                    vm.ModificadoPor = config.CriadoPor;
                    vm.ModificadoPorNome = (from u in db.Usuario where u.FilialId == filialId && u.Login == config.CriadoPor select u.Nome).FirstOrDefault();
                    vm.ModificadoEm = config.CriadoEm;
                }
            }

            return PartialView("_ConfigUpload", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConfigUpload(ConfigUploadViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return PartialView("_ConfigUpload", vm);
            }

            ViewBag.ControllerName = ControllerContext.RouteData.Values["controller"].ToString();
            ViewBag.ActionName = ControllerContext.RouteData.Values["action"].ToString();

            using (DbContextTransaction tr = db.Database.BeginTransaction())
            {
                try
                {
                    if (vm.Valor == null || vm.Valor == string.Empty)
                    {
                        AppConfig config = db.AppConfig.Find(vm.Id);
                        if (config != null)
                        {
                            db.AppConfig.Remove(config);
                        }
                    }
                    else
                    {
                        AppConfig config = db.AppConfig.Find(vm.Id);
                        if (config == null)
                        {
                            AppConfig new_cfg = new AppConfig();
                            new_cfg.Nome = "ExcludeLoc";
                            new_cfg.Descricao = "Locações a serem desconsideradas na atualização do arquivo de inventário";
                            new_cfg.Valor = vm.Valor;
                            new_cfg.CriadoPor = Util.GetCurrentUser();
                            new_cfg.CriadoEm = Util.GetCurrentDateTime();
                            db.AppConfig.Add(new_cfg);
                        }
                        else
                        {
                            config.Valor = vm.Valor;
                            config.ModificadoPor = Util.GetCurrentUser();
                            config.ModificadoEm = Util.GetCurrentDateTime();
                            db.Entry(config).State = EntityState.Modified;
                        }
                    }

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
                    return PartialView("_ConfigUpload", vm);
                }
                catch (Exception ex)
                {
                    ViewBag.Exception = ex.Message;
                    ViewBag.InnerException = ex.InnerException;
                    ViewBag.Source = ex.Source;
                    tr.Rollback();
                    return PartialView("_ConfigUpload", vm);
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
