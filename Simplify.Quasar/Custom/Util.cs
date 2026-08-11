using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using Microsoft.Ajax.Utilities;
using Simplify.Quasar.Areas.EstoqueApp.ViewModels;
using Simplify.Quasar.Models;
using Simplify.Quasar.ViewModels;

namespace Simplify.Quasar.Custom
{
    public class Util
    {
        private static readonly object MenuCacheVersionLock = new object();
        private static readonly object PerfilAreaSchemaLock = new object();
        private const string MenuCacheVersionKeyPrefix = "menu-version-profile-";
        private const int ConfiguracaoMenuId = 29;
        private static bool PerfilAreaSchemaInitialized;

        public static bool IsTestEnvironment()
        {
            var environment = ConfigurationManager.AppSettings["Environment"];
            return environment != null && environment.ToLower() == "test";
        }

        public static string GetSessionCulture()
        {
            var currentCulture = Thread.CurrentThread.CurrentCulture;
            var session = HttpContext.Current != null ? HttpContext.Current.Session : null;

            if (session == null)
            {
                return currentCulture.Name;
            }

            var culture = session["lang"] as string;
            if (string.IsNullOrWhiteSpace(culture))
            {
                culture = currentCulture.Name;
                session["lang"] = culture;
            }

            return culture;
        }

        public static string GetCurrentUser()
        {
            var session = HttpContext.Current != null ? HttpContext.Current.Session : null;
            return session?["useraccount"] as string ?? string.Empty;
        }

        public static int GetCurrentFilial()
        {
            var session = HttpContext.Current != null ? HttpContext.Current.Session : null;
            int filialId;
            return int.TryParse(session?["filialid"]?.ToString(), out filialId) ? filialId : 0;
        }

        public static int GetPerfilId()
        {
            var session = HttpContext.Current != null ? HttpContext.Current.Session : null;
            int perfilId;
            return int.TryParse(session?["perfilid"]?.ToString(), out perfilId) ? perfilId : 0;
        }

        public static bool IsAdminProfile()
        {
            return GetPerfilId() == 1;
        }

        public static bool IsAdminUser()
        {
            return string.Equals(GetCurrentUser(), "admin", StringComparison.OrdinalIgnoreCase);
        }

        public static int GetPeriodoRecebimento()
        {
            using (var db = new Quasar_Entities())
            {
                int filialId = GetCurrentFilial();
                int periodo = db.AppConfig
                    .Where(m =>
                        m.Nome == "PeriodoRecebimento" &&
                        (m.FilialId == filialId || m.FilialId == null))
                    .OrderByDescending(m => m.FilialId == filialId)
                    .ThenByDescending(m => m.Id)
                    .Select(m => m.Valor)
                    .AsEnumerable()
                    .Select(v => int.TryParse(v, out var x) ? x : (int?)null)
                    .FirstOrDefault() ?? 30;

                return Math.Max(0, periodo);
            }
        }

        public static int GetPeriodoExpedicao()
        {
            using (var db = new Quasar_Entities())
            {
                return db.AppConfig
                    .Where(m => m.Nome == "PeriodoExpedicao")
                    .Select(m => m.Valor)
                    .AsEnumerable()
                    .Select(v => int.TryParse(v, out var x) ? x : (int?)null)
                    .FirstOrDefault() ?? 15;
            }
        }

        public static int GetOnlineUserTimeoutMinutes(int filialId)
        {
            const int defaultMinutes = 2;

            using (var db = new Quasar_Entities())
            {
                string value = db.AppConfig
                    .Where(x =>
                        x.Nome == "UsuariosAtivosTempoLimiteMinutos" &&
                        (x.FilialId == filialId || x.FilialId == null))
                    .OrderByDescending(x => x.FilialId == filialId)
                    .Select(x => x.Valor)
                    .FirstOrDefault();

                int minutes;
                return int.TryParse(value, out minutes) && minutes > 0
                    ? minutes
                    : defaultMinutes;
            }
        }

        public static void EnsureOnlineUserTimeoutParameters()
        {
            const string nome = "UsuariosAtivosTempoLimiteMinutos";

            using (var db = new Quasar_Entities())
            {
                List<int> filialIds = db.Empresa
                    .Select(x => x.Id)
                    .ToList();
                HashSet<int> filialIdsConfiguradas = new HashSet<int>(
                    db.AppConfig
                        .Where(x => x.Nome == nome && x.FilialId.HasValue)
                        .Select(x => x.FilialId.Value)
                        .ToList());
                DateTime agora = GetCurrentDateTime();

                foreach (int filialId in filialIds.Where(x => !filialIdsConfiguradas.Contains(x)))
                {
                    db.AppConfig.Add(new AppConfig
                    {
                        Nome = nome,
                        Descricao = "Tempo máximo, em minutos, sem atividade para considerar uma sessão de usuário online.",
                        Valor = "2",
                        CriadoPor = "sistema",
                        CriadoEm = agora,
                        FilialId = filialId
                    });
                }

                db.SaveChanges();
            }
        }

        public static DateTime GetCurrentDateTime()
        {
            DateTime result;

            try
            {
                DateTime utc = DateTime.UtcNow;
                TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
                result = TimeZoneInfo.ConvertTimeFromUtc(utc, tz);
            }
            catch (Exception)
            {
                result = DateTime.Now;
            }

            return result;
        }

        public static DateTime ConvertUtcToApplicationTime(DateTime utcDate)
        {
            try
            {
                DateTime normalizedUtc = utcDate.Kind == DateTimeKind.Utc
                    ? utcDate
                    : DateTime.SpecifyKind(utcDate, DateTimeKind.Utc);

                TimeZoneInfo timeZone =
                    TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");

                return TimeZoneInfo.ConvertTimeFromUtc(normalizedUtc, timeZone);
            }
            catch (Exception)
            {
                return utcDate.ToLocalTime();
            }
        }

        public static DateTime ConvertToLocalDate(DateTime? date)
        {
            DateTime result;

            try
            {
                result = TimeZoneInfo.ConvertTime((DateTime)date, TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time"));
            }
            catch (Exception)
            {
                result = (DateTime)date;
            }

            return result;
        }

        public static MaterialViewModel GetMaterial(string codigo)
        {
            using (var db = new Quasar_Entities())
            {
                var material = (from m in db.Material
                                where m.Codigo == codigo
                                select new MaterialViewModel
                                {
                                    Codigo = m.Codigo,
                                    Descricao = m.Descricao,
                                    UN = m.UN,
                                    EmbalagemMin = m.EmbalagemMin,
                                    MediaVendas = m.MediaVendas,
                                    CustoUnitario = m.CustoUnitario,
                                    Curva = m.Curva,
                                    CriadoEm = m.CriadoEm,
                                    CriadoPor = m.CriadoPor,
                                    ModificadoEm = m.ModificadoEm,
                                    ModificadoPor = m.ModificadoPor
                                }).FirstOrDefault();

                if (material == null)
                {
                    return new MaterialViewModel();
                }

                return material;
            }
        }

        public static List<SP_GetItensEstoque_Result> GetItensEstoque(int filialId, Quasar_Entities db)
        {
            return db.Database.SqlQuery<SP_GetItensEstoque_Result>(@"
SELECT
    t1.Id,
    ISNULL(t1.Locacao, '') AS Locacao,
    t1.ItemNr,
    ISNULL(t2.Descricao, '') AS Descricao,
    ISNULL(t1.Saldo, 0) AS Saldo,
    ISNULL(t1.Indisponivel, 0) AS Indisponivel,
    ISNULL(t1.PedidoPendente, 0) AS PedidoPendente,
    ISNULL(t1.ValorEstoque, 0) AS ValorEstoque,
    ISNULL(t1.[Range], '') AS [Range],
    t1.ModificadoEm
FROM Estoque t1
LEFT JOIN Material t2 ON t2.Codigo = t1.ItemNr
WHERE t1.FilialId = @p0", filialId).ToList();
        }

        // Lista permissões CRUD de um determinado perfil para um componente.
        public static string GetPermissoes(string componente)
        {
            return GetPermissoes(componente, null);
        }

        public static string GetPermissoes(string componente, string area)
        {
            int perfilId = GetPerfilId();
            if (perfilId == 0 || string.IsNullOrWhiteSpace(componente))
            {
                return string.Empty;
            }

            if (perfilId == 1)
            {
                return "[add][update][delete][view]";
            }

            using (var db = new Quasar_Entities())
            {
                var funcoes = (from pf in db.PerfilFuncao
                               join f in db.AppFuncao on pf.IdFuncao equals f.Id
                               where pf.IdPerfil == perfilId
                                  && pf.Status == true
                                  && f.Status == true
                                  && (f.CodComponente == componente || f.Controller == componente)
                               select new
                               {
                                   f.Codigo,
                                   f.Action,
                                   MenuArea = (from m in db.AppMenu
                                               where m.Id == f.IdMenu
                                               select m.Area).FirstOrDefault()
                               }).ToList();

                var permissoes = funcoes
                    .Where(f => string.IsNullOrWhiteSpace(area) || string.IsNullOrWhiteSpace(f.MenuArea) || f.MenuArea == area)
                    .Select(f => GetCrudPermission(f.Action, f.Codigo))
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .Distinct()
                    .ToList();

                if (permissoes.Count == 0)
                {
                    return string.Empty;
                }

                return string.Concat(permissoes.Select(p => "[" + p + "]"));
            }
        }

        public static IEnumerable<SelectListItem> GetEstadoDDL(string uf)
        {
            Quasar_Entities db = new Quasar_Entities();

            var ddl = (from item in db.Estado
                       select new SelectListItem
                       {
                           Value = item.UF,
                           Text = item.Nome,
                           Selected = item.UF == uf
                       }).ToList();

            db.Dispose();

            return ddl.OrderBy(x => x.Text);
        }

        public static IEnumerable<SelectListItem> GetParadaDDL(int? filial, int? id)
        {
            Quasar_Entities db = new Quasar_Entities();

            var ddl = (from item in db.Parada
                       where item.FilialId == filial
                       select new SelectListItem
                       {
                           Value = item.Id.ToString(),
                           Text = item.Descricao,
                           Selected = item.Id == id
                       }).ToList();

            db.Dispose();

            return ddl.OrderBy(x => x.Text);
        }

        public static IEnumerable<SelectListItem> GetStatusNotaFiscal(int? id)
        {
            Quasar_Entities db = new Quasar_Entities();

            var ddl = (from item in db.StatusNotaFiscal
                       select new SelectListItem
                       {
                           Value = item.Id.ToString(),
                           Text = item.Descricao,
                           Selected = item.Id == id
                       }).ToList();

            db.Dispose();

            return ddl.OrderBy(x => x.Text);
        }

        public static IEnumerable<SelectListItem> GetStatusDocExpedicao(int? id)
        {
            Quasar_Entities db = new Quasar_Entities();

            var ddl = (from item in db.StatusDocExpedicao
                       where item.Id != 1
                       select new SelectListItem
                       {
                           Value = item.Id.ToString(),
                           Text = item.Nome,
                           Selected = item.Id == id
                       }).ToList();

            db.Dispose();

            return ddl.OrderBy(x => x.Text);
        }

        // Obter DDL de empresas
        public static IEnumerable<SelectListItem> GetEmpresas(int? id)
        {
            Quasar_Entities db = new Quasar_Entities();

            var ddl = (from item in db.Empresa
                       select new SelectListItem
                       {
                           Value = item.Id.ToString(),
                           Text = item.Nome,
                           Selected = item.Id == id
                       }).ToList();

            db.Dispose();

            return ddl.OrderBy(x => x.Text);
        }

        public static int? GetEmpresaSorocabaId()
        {
            using (var db = new Quasar_Entities())
            {
                var sorocabaId = db.Empresa
                    .Select(item => new
                    {
                        item.Id,
                        item.Nome
                    })
                    .ToList()
                    .Where(item => !string.IsNullOrWhiteSpace(item.Nome)
                        && item.Nome.IndexOf("SOROCABA", StringComparison.OrdinalIgnoreCase) >= 0)
                    .Select(item => (int?)item.Id)
                    .FirstOrDefault();

                return sorocabaId
                    ?? db.Empresa
                        .OrderBy(item => item.Id)
                        .Select(item => (int?)item.Id)
                        .FirstOrDefault();
            }
        }

        //public static IEnumerable<SelectListItem> GetFuncao(int? id)
        //{
        //    Quasar_Entities db = new Quasar_Entities();

        //    var ddl = (from item in db.Funcao
        //               select new SelectListItem
        //               {
        //                   Value = item.Id.ToString(),
        //                   Text = item.Nome,
        //                   Selected = item.Id == id
        //               }).ToList();

        //    db.Dispose();

        //    return ddl.OrderBy(x => x.Text);
        //}

        public static IEnumerable<SelectListItem> GetTipoAreaDDL(int? id, int? filial)
        {
            Quasar_Entities db = new Quasar_Entities();

            var ddl = (from item in db.TipoArea
                       where item.FilialId == filial
                       select new SelectListItem
                       {
                           Value = item.Id.ToString(),
                           Text = item.Descricao,
                           Selected = item.Id == (id ?? 0)
                       }).ToList();

            db.Dispose();

            return ddl.OrderBy(x => x.Text);
        }

        public static IEnumerable<SelectListItem> GetAreaDDL(int? filial, bool Etiqueta)
        {
            Quasar_Entities db = new Quasar_Entities();

            var ddl = (from item in db.Area
                       where item.FilialId == filial
                       select new SelectListItem
                       {
                           Value = item.Id.ToString(),
                           Text = item.Nome,
                           Selected = item.Etiqueta == Etiqueta
                       }).ToList();

            db.Dispose();

            return ddl.OrderBy(x => x.Text);
        }

        // Obter DDL de área
        public static IEnumerable<SelectListItem> GetAreas(int? filial, int? id)
        {
            Quasar_Entities db = new Quasar_Entities();

            var ddl = (from item in db.Area
                       where item.FilialId == filial
                       select new SelectListItem
                       {
                           Value = item.Id.ToString(),
                           Text = item.Nome,
                           Selected = item.Id == id
                       }).ToList();

            db.Dispose();

            return ddl.OrderBy(x => x.Text);
        }

        // Obter DDL de clientes
        public static IEnumerable<SelectListItem> GetClientes(int? id)
        {
            Quasar_Entities db = new Quasar_Entities();

            var ddl = (from item in db.Cliente
                       select new SelectListItem
                       {
                           Value = item.Id.ToString(),
                           Text = item.Nome,
                           Selected = item.Id == id
                       }).ToList();

            db.Dispose();

            return ddl.OrderBy(x => x.Text);
        }

        // Obter DDL de estados
        public static IEnumerable<SelectListItem> GetEstados(string uf)
        {
            Quasar_Entities db = new Quasar_Entities();

            var ddl = (from item in db.Estado
                       select new SelectListItem
                       {
                           Value = item.UF,
                           Text = item.Nome,
                           Selected = item.UF == uf
                       }).ToList();

            db.Dispose();

            return ddl.OrderBy(x => x.Text);
        }

        // Obter DDL de perfis de acesso
        public static IEnumerable<SelectListItem> GetPerfisUsuario(int? id, bool includeAdminProfile = true)
        {
            Quasar_Entities db = new Quasar_Entities();

            var ddl = (from item in db.PerfilUsuario
                       where includeAdminProfile || item.Id != 1
                       select new SelectListItem
                       {
                           Value = item.Id.ToString(),
                           Text = item.Descricao,
                           Selected = item.Id == id
                       }).ToList();

            db.Dispose();

            return ddl.OrderBy(x => x.Text);
        }

        public static IEnumerable<SelectListItem> GetTransportadoraDDL(int? filial, int? id)
        {
            Quasar_Entities db = new Quasar_Entities();

            var ddl = (from item in db.Transportadora
                       where item.FilialId == filial
                       select new SelectListItem
                       {
                           Value = item.Id.ToString(),
                           Text = item.Nome_Fantasia,
                           Selected = item.Id == id
                       }).ToList();

            db.Dispose();

            return ddl.OrderBy(x => x.Text);
        }

        public static IEnumerable<SelectListItem> GetTipoMovimentoExpedicaoDDL(int? id)
        {
            Quasar_Entities db = new Quasar_Entities();

            var ddl = (from item in db.TipoMovimentoExpedicao
                       select new SelectListItem
                       {
                           Value = item.Id.ToString(),
                           Text = item.Descricao,
                           Selected = item.Id == id
                       }).ToList();

            db.Dispose();

            return ddl.OrderBy(x => x.Text);
        }

        public static IEnumerable<SelectListItem> GetRotaDDL(int? filial, int? id)
        {
            Quasar_Entities db = new Quasar_Entities();

            var ddl = (from item in db.Rota
                       where item.FilialId == filial
                       select new SelectListItem
                       {
                           Value = item.Id.ToString(),
                           Text = item.Nome,
                           Selected = item.Id == id
                       }).ToList();

            db.Dispose();

            return ddl.OrderBy(x => x.Text);
        }

        public static IEnumerable<SelectListItem> GetTipoDocumentoRetornoDDL(int filial, int? id)
        {
            Quasar_Entities db = new Quasar_Entities();

            var ddl = (from item in db.TipoDocumentoRetorno
                       //where item.FilialId == filial
                       select new SelectListItem
                       {
                           Value = item.Id.ToString(),
                           Text = item.Descricao,
                           Selected = item.Id == id
                       }).ToList();

            db.Dispose();

            return ddl.OrderBy(x => x.Value);
        }

        public static IEnumerable<SelectListItem> GetLocalOrigemDDL(int filial, int? id)
        {
            Quasar_Entities db = new Quasar_Entities();

            var ddl = (from item in db.LocalOrigem
                       //where item.FilialId == filial
                       select new SelectListItem
                       {
                           Value = item.Id.ToString(),
                           Text = item.Nome,
                           Selected = item.Id == id
                       }).ToList();

            db.Dispose();

            return ddl.OrderBy(x => x.Value);
        }

        public static IEnumerable<SelectListItem> GetLocalDestinoDDL(int filial, int? id)
        {
            Quasar_Entities db = new Quasar_Entities();

            var ddl = (from item in db.LocalDestino
                       //where item.FilialId == filial
                       select new SelectListItem
                       {
                           Value = item.Id.ToString(),
                           Text = item.Nome,
                           Selected = item.Id == id
                       }).ToList();

            db.Dispose();

            return ddl.OrderBy(x => x.Value);
        }

        public static string FormatSQL(string sql)
        {
            string current_user = GetCurrentUser();
            string currentDate = GetCurrentDateTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            string currentDateSql = "CONVERT(datetime, '" + currentDate + "', 120)";

            // Os servidores Web e SQL podem estar configurados em fusos distintos.
            // Os comandos operacionais da AppSQL devem usar o mesmo horario local
            // calculado pela aplicacao, em vez do relogio local do SQL Server.
            sql = Regex.Replace(
                sql,
                @"\bGETDATE\s*\(\s*\)",
                currentDateSql,
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            sql = sql.Replace("@data_sistema", currentDate);
            sql = sql.Replace("@usuario_sistema", current_user);
            sql = sql.Replace("_usuario_", current_user);
            sql = sql.Replace("@filial", GetCurrentFilial().ToString());
            return sql;
        }

        public static string FormatCNPJ(string CNPJ)
        {
            return Convert.ToUInt64(CNPJ).ToString(@"00\.000\.000\/0000\-00");
        }

        public static string FormatCPF(string CPF)
        {
            return Convert.ToUInt64(CPF).ToString(@"000\.000\.000\-00");
        }

        public static string SemFormatacao(string codigo)
        {
            return codigo.Replace(".", string.Empty).Replace("-", string.Empty).Replace("/", string.Empty);
        }

        public static string RemoverAcentuacao(string texto)
        {
            return new string(texto
                .Normalize(NormalizationForm.FormD)
                .Where(ch => char.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                .ToArray());
        }

        public static bool IsValid(string cpfCnpj)
        {
            return (IsCpf(cpfCnpj) || IsCnpj(cpfCnpj));
        }

        private static bool IsCpf(string cpf)
        {
            int[] multiplicador1 = new int[9] { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
            int[] multiplicador2 = new int[10] { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };

            cpf = cpf.Trim().Replace(".", "").Replace("-", "");
            if (cpf.Length != 11)
                return false;

            for (int j = 0; j < 10; j++)
                if (j.ToString().PadLeft(11, char.Parse(j.ToString())) == cpf)
                    return false;

            string tempCpf = cpf.Substring(0, 9);
            int soma = 0;

            for (int i = 0; i < 9; i++)
                soma += int.Parse(tempCpf[i].ToString()) * multiplicador1[i];

            int resto = soma % 11;
            if (resto < 2)
                resto = 0;
            else
                resto = 11 - resto;

            string digito = resto.ToString();
            tempCpf = tempCpf + digito;
            soma = 0;
            for (int i = 0; i < 10; i++)
                soma += int.Parse(tempCpf[i].ToString()) * multiplicador2[i];

            resto = soma % 11;
            if (resto < 2)
                resto = 0;
            else
                resto = 11 - resto;

            digito = digito + resto.ToString();

            return cpf.EndsWith(digito);
        }

        private static bool IsCnpj(string cnpj)
        {
            int[] multiplicador1 = new int[12] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
            int[] multiplicador2 = new int[13] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

            cnpj = cnpj.Trim().Replace(".", "").Replace("-", "").Replace("/", "");
            if (cnpj.Length != 14)
                return false;

            string tempCnpj = cnpj.Substring(0, 12);
            int soma = 0;

            for (int i = 0; i < 12; i++)
                soma += int.Parse(tempCnpj[i].ToString()) * multiplicador1[i];

            int resto = (soma % 11);
            if (resto < 2)
                resto = 0;
            else
                resto = 11 - resto;

            string digito = resto.ToString();
            tempCnpj = tempCnpj + digito;
            soma = 0;
            for (int i = 0; i < 13; i++)
                soma += int.Parse(tempCnpj[i].ToString()) * multiplicador2[i];

            resto = (soma % 11);
            if (resto < 2)
                resto = 0;
            else
                resto = 11 - resto;

            digito = digito + resto.ToString();

            return cnpj.EndsWith(digito);
        }

        // Validar senha
        public static bool ValidatePassword(string password, string correctHash)
        {
            return BCrypt.Net.BCrypt.Verify(password, correctHash);
        }

        // Converter senha (string) em hash
        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, GetRandomSalt());
        }

        // Gerar hash
        private static string GetRandomSalt()
        {
            return BCrypt.Net.BCrypt.GenerateSalt(12);
        }

        // Criptografar Base64
        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }

        // Descriptografar Base64
        public static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
            return Encoding.UTF8.GetString(base64EncodedBytes);
        }

        // Criptografar AES
        public static string StringToCrypto(string pwd)
        {
            string EncryptionKey = "MAKV2SPBNI99212";
            byte[] clearBytes = Encoding.Unicode.GetBytes(pwd);
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(clearBytes, 0, clearBytes.Length);
                        cs.Close();
                    }
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        // Descriptografar AES
        public static string CryptoToString(string pwd)
        {
            string EncryptionKey = "MAKV2SPBNI99212";
            byte[] cipherBytes = Convert.FromBase64String(pwd);
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(cipherBytes, 0, cipherBytes.Length);
                        cs.Close();
                    }
                    return Encoding.Unicode.GetString(ms.ToArray());
                }
            }
        }

        public static IEnumerable<SelectListItem> GetAppMenuDDL(int? id)
        {
            Quasar_Entities db = new Quasar_Entities();

            var ddl = (from item in db.AppMenu
                       where item.Status == true
                       orderby item.Titulo
                       select new SelectListItem
                       {
                           Value = item.Id.ToString(),
                           Text = item.Titulo,
                           Selected = item.Id == id
                       }).ToList();

            db.Dispose();

            return ddl;
        }

        public static IEnumerable<SelectListItem> GetAppComponenteDDL(int? id)
        {
            Quasar_Entities db = new Quasar_Entities();

            var ddl = (from item in db.AppComponente
                       where item.Status == true
                       orderby item.Codigo
                       select new SelectListItem
                       {
                           Value = item.Codigo,
                           Text = item.DescPTBR,
                           Selected = item.Codigo == (id.HasValue ? item.Codigo : null)
                       }).ToList();

            db.Dispose();

            return ddl;
        }

        public static void EnsureRecebimentoConferenciaVolumeMenuTarget()
        {
            using (var db = new Quasar_Entities())
            {
                var menu = db.AppMenu.FirstOrDefault(x => x.Id == 72);
                if (menu == null)
                {
                    return;
                }

                const string controller = "Pendencias";
                const string action = "ConferenciaVolume";

                if (string.Equals(menu.Controller, controller, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(menu.Action, action, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                menu.Controller = controller;
                menu.Action = action;
                menu.DatUltAtlz = GetCurrentDateTime();

                db.Entry(menu).State = EntityState.Modified;
                db.SaveChanges();

                foreach (var perfilId in db.PerfilUsuario.Select(x => x.Id).ToList())
                {
                    InvalidateMenuCache(perfilId);
                }
            }
        }

        public static void EnsureControleAcessoAtividadesMenu()
        {
            using (var db = new Quasar_Entities())
            {
                const string titulo = "Atividades";
                const string area = "ControleAcessoApp";
                const string controller = "Atividade";
                const string action = "Index";
                const string css = "fa-solid fa-users-viewfinder";
                const int sequencia = 1030;
                const int nivel = 2;

                var menuPai = db.AppMenu
                    .FirstOrDefault(x =>
                        x.IdNivelSup == null
                        && x.Area == area
                        && (x.Titulo == "Controle de Acesso" || x.Titulo == "Controle de Acessos"));

                if (menuPai == null)
                {
                    return;
                }

                var menu = db.AppMenu.FirstOrDefault(x =>
                    x.Area == area
                    && x.Controller == controller
                    && x.Action == action);

                bool changed = false;
                if (menu == null)
                {
                    menu = new AppMenu
                    {
                        Titulo = titulo,
                        Area = area,
                        Controller = controller,
                        Action = action,
                        Css = css,
                        Status = true,
                        Sequencia = sequencia,
                        Nivel = nivel,
                        IdNivelSup = menuPai.Id,
                        HasChild = false,
                        DatUltAtlz = GetCurrentDateTime(),
                        FilialId = menuPai.FilialId
                    };

                    db.AppMenu.Add(menu);
                    changed = true;
                }
                else
                {
                    if (menu.Titulo != titulo) { menu.Titulo = titulo; changed = true; }
                    if (menu.Css != css) { menu.Css = css; changed = true; }
                    if (!menu.Status) { menu.Status = true; changed = true; }
                    if (menu.Sequencia != sequencia) { menu.Sequencia = sequencia; changed = true; }
                    if (menu.Nivel != nivel) { menu.Nivel = nivel; changed = true; }
                    if (menu.IdNivelSup != menuPai.Id) { menu.IdNivelSup = menuPai.Id; changed = true; }
                    if (menu.HasChild) { menu.HasChild = false; changed = true; }
                    if (menu.FilialId != menuPai.FilialId) { menu.FilialId = menuPai.FilialId; changed = true; }

                    if (changed)
                    {
                        menu.DatUltAtlz = GetCurrentDateTime();
                        db.Entry(menu).State = EntityState.Modified;
                    }
                }

                if (!changed)
                {
                    return;
                }

                db.SaveChanges();
                foreach (var perfilId in db.PerfilUsuario.Select(x => x.Id).ToList())
                {
                    InvalidateMenuCache(perfilId);
                }
            }
        }

        public static void EnsureExpedicaoConferenciaRomaneioMenu()
        {
            using (var db = new Quasar_Entities())
            {
                const string titulo = "Conferir Romaneios";
                const string area = "ExpedicaoApp";
                const string controller = "Home";
                const string action = "ConferenciaRomaneios";
                const string css = "fa-solid fa-check";
                const int sequencia = 401;
                const int nivel = 2;
                const int menuPaiId = 24;

                var menu = db.AppMenu
                    .FirstOrDefault(x => x.IdNivelSup == menuPaiId && x.Area == area && x.Controller == controller && x.Action == action)
                    ?? db.AppMenu.FirstOrDefault(x => x.Titulo == titulo && x.Area == area);

                bool changed = false;

                if (menu == null)
                {
                    menu = new AppMenu
                    {
                        Titulo = titulo,
                        Area = area,
                        Controller = controller,
                        Action = action,
                        Css = css,
                        Status = true,
                        Sequencia = sequencia,
                        Nivel = nivel,
                        IdNivelSup = menuPaiId,
                        HasChild = false,
                        DatUltAtlz = GetCurrentDateTime()
                    };

                    db.AppMenu.Add(menu);
                    changed = true;
                }
                else
                {
                    if (!string.Equals(menu.Titulo, titulo, StringComparison.Ordinal))
                    {
                        menu.Titulo = titulo;
                        changed = true;
                    }

                    if (!string.Equals(menu.Area, area, StringComparison.OrdinalIgnoreCase))
                    {
                        menu.Area = area;
                        changed = true;
                    }

                    if (!string.Equals(menu.Controller, controller, StringComparison.OrdinalIgnoreCase))
                    {
                        menu.Controller = controller;
                        changed = true;
                    }

                    if (!string.Equals(menu.Action, action, StringComparison.OrdinalIgnoreCase))
                    {
                        menu.Action = action;
                        changed = true;
                    }

                    if (!string.Equals(menu.Css, css, StringComparison.Ordinal))
                    {
                        menu.Css = css;
                        changed = true;
                    }

                    if (!menu.Status)
                    {
                        menu.Status = true;
                        changed = true;
                    }

                    if (menu.Sequencia != sequencia)
                    {
                        menu.Sequencia = sequencia;
                        changed = true;
                    }

                    if (menu.Nivel != nivel)
                    {
                        menu.Nivel = nivel;
                        changed = true;
                    }

                    if (menu.IdNivelSup != menuPaiId)
                    {
                        menu.IdNivelSup = menuPaiId;
                        changed = true;
                    }

                    if (menu.HasChild)
                    {
                        menu.HasChild = false;
                        changed = true;
                    }

                    if (changed)
                    {
                        menu.DatUltAtlz = GetCurrentDateTime();
                        db.Entry(menu).State = EntityState.Modified;
                    }
                }

                if (!changed)
                {
                    return;
                }

                db.SaveChanges();

                foreach (var perfilId in db.PerfilUsuario.Select(x => x.Id).ToList())
                {
                    InvalidateMenuCache(perfilId);
                }
            }
        }

        public static void EnsureSeparacaoDashboardMenu()
        {
            using (var db = new Quasar_Entities())
            {
                const string titulo = "Dashboard";
                const string area = "SeparacaoApp";
                const string controller = "Home";
                const string action = "Dashboard";
                const string css = "fa-solid fa-chart-line";
                const int sequencia = 305;
                const int nivel = 2;
                const int menuPaiId = 56;

                var menu = db.AppMenu
                    .FirstOrDefault(x => x.IdNivelSup == menuPaiId && x.Area == area && x.Controller == controller && x.Action == action)
                    ?? db.AppMenu.FirstOrDefault(x => x.Titulo == titulo && x.Area == area);

                bool changed = false;

                if (menu == null)
                {
                    menu = new AppMenu
                    {
                        Titulo = titulo,
                        Area = area,
                        Controller = controller,
                        Action = action,
                        Css = css,
                        Status = true,
                        Sequencia = sequencia,
                        Nivel = nivel,
                        IdNivelSup = menuPaiId,
                        HasChild = false,
                        DatUltAtlz = GetCurrentDateTime(),
                        FilialId = 1
                    };

                    db.AppMenu.Add(menu);
                    changed = true;
                }
                else
                {
                    if (!string.Equals(menu.Titulo, titulo, StringComparison.Ordinal))
                    {
                        menu.Titulo = titulo;
                        changed = true;
                    }

                    if (!string.Equals(menu.Area, area, StringComparison.OrdinalIgnoreCase))
                    {
                        menu.Area = area;
                        changed = true;
                    }

                    if (!string.Equals(menu.Controller, controller, StringComparison.OrdinalIgnoreCase))
                    {
                        menu.Controller = controller;
                        changed = true;
                    }

                    if (!string.Equals(menu.Action, action, StringComparison.OrdinalIgnoreCase))
                    {
                        menu.Action = action;
                        changed = true;
                    }

                    if (!string.Equals(menu.Css, css, StringComparison.Ordinal))
                    {
                        menu.Css = css;
                        changed = true;
                    }

                    if (!menu.Status)
                    {
                        menu.Status = true;
                        changed = true;
                    }

                    if (menu.Sequencia != sequencia)
                    {
                        menu.Sequencia = sequencia;
                        changed = true;
                    }

                    if (menu.Nivel != nivel)
                    {
                        menu.Nivel = nivel;
                        changed = true;
                    }

                    if (menu.IdNivelSup != menuPaiId)
                    {
                        menu.IdNivelSup = menuPaiId;
                        changed = true;
                    }

                    if (menu.HasChild)
                    {
                        menu.HasChild = false;
                        changed = true;
                    }

                    if (menu.FilialId != 1)
                    {
                        menu.FilialId = 1;
                        changed = true;
                    }

                    if (changed)
                    {
                        menu.DatUltAtlz = GetCurrentDateTime();
                        db.Entry(menu).State = EntityState.Modified;
                    }
                }

                if (!changed)
                {
                    return;
                }

                db.SaveChanges();

                foreach (var perfilId in db.PerfilUsuario.Select(x => x.Id).ToList())
                {
                    InvalidateMenuCache(perfilId);
                }
            }
        }

        public static void EnsureEstoqueAssociacaoLocacaoMenu()
        {
            using (var db = new Quasar_Entities())
            {
                const string titulo = "Definir Item por Locação";
                const string area = "EstoqueApp";
                const string controller = "AssociacaoLocacao";
                const string action = "Index";
                const string css = "fa-solid fa-map-location-dot";
                const int sequencia = 270;
                const int nivel = 2;
                const int menuPaiId = 23;

                var menu = db.AppMenu.FirstOrDefault(x =>
                    x.IdNivelSup == menuPaiId &&
                    x.Area == area &&
                    x.Controller == controller &&
                    x.Action == action);

                bool changed = false;
                if (menu == null)
                {
                    menu = new AppMenu
                    {
                        Titulo = titulo,
                        Area = area,
                        Controller = controller,
                        Action = action,
                        Css = css,
                        Status = true,
                        Sequencia = sequencia,
                        Nivel = nivel,
                        IdNivelSup = menuPaiId,
                        HasChild = false,
                        DatUltAtlz = GetCurrentDateTime()
                    };

                    db.AppMenu.Add(menu);
                    changed = true;
                }
                else
                {
                    if (menu.Titulo != titulo) { menu.Titulo = titulo; changed = true; }
                    if (menu.Css != css) { menu.Css = css; changed = true; }
                    if (!menu.Status) { menu.Status = true; changed = true; }
                    if (menu.Sequencia != sequencia) { menu.Sequencia = sequencia; changed = true; }
                    if (menu.Nivel != nivel) { menu.Nivel = nivel; changed = true; }
                    if (menu.IdNivelSup != menuPaiId) { menu.IdNivelSup = menuPaiId; changed = true; }
                    if (menu.HasChild) { menu.HasChild = false; changed = true; }

                    if (changed)
                    {
                        menu.DatUltAtlz = GetCurrentDateTime();
                        db.Entry(menu).State = EntityState.Modified;
                    }
                }

                if (!changed)
                {
                    return;
                }

                db.SaveChanges();
                foreach (var perfilId in db.PerfilUsuario.Select(x => x.Id).ToList())
                {
                    InvalidateMenuCache(perfilId);
                }
            }
        }

        public static List<int> GetMenuIdsByPerfil(int perfilId, Quasar_Entities db, string area = null)
        {
            var areasPermitidas = perfilId == 1
                ? null
                : GetAllowedAreasByPerfil(perfilId, db);

            var ids = (from m in db.AppMenu
                       where m.Status == true
                          && (area == null || m.Area == area)
                       select new
                       {
                           m.Id,
                           m.Area
                       }).ToList()
                       .Where(m => areasPermitidas == null
                           || string.IsNullOrWhiteSpace(m.Area)
                           || areasPermitidas.Contains(m.Area))
                       .Select(m => m.Id)
                       .Distinct()
                       .ToList();

            return ids;
        }

        public static HashSet<string> GetAllowedAreasByPerfil(int perfilId, Quasar_Entities db)
        {
            EnsurePerfilAreaAccessSchema(db);

            var areas = db.Database
                .SqlQuery<string>(
                    "SELECT Area FROM dbo.PerfilAreaAcesso WHERE PerfilId = @p0 AND Status = 1",
                    perfilId)
                .Where(a => !string.IsNullOrWhiteSpace(a) && !IsIgnoredPerfilArea(a))
                .Select(a => a.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return new HashSet<string>(areas, StringComparer.OrdinalIgnoreCase);
        }

        public static void SaveAllowedAreasByPerfil(int perfilId, IEnumerable<string> areas, Quasar_Entities db)
        {
            EnsurePerfilAreaAccessSchema(db);

            var areasAtivas = GetActiveAreas(db);
            var selecionadas = (areas ?? Enumerable.Empty<string>())
                .Where(a => !string.IsNullOrWhiteSpace(a))
                .Select(a => a.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(a => areasAtivas.Contains(a, StringComparer.OrdinalIgnoreCase))
                .ToList();

            string usuario = GetCurrentUser();
            if (string.IsNullOrWhiteSpace(usuario))
            {
                usuario = "SYSTEM";
            }

            DateTime dataAtual = GetCurrentDateTime();

            db.Database.ExecuteSqlCommand(
                "DELETE FROM dbo.PerfilAreaAcesso WHERE PerfilId = @p0",
                perfilId);

            foreach (var area in selecionadas)
            {
                InsertPerfilAreaAccessRow(db, perfilId, area, usuario, dataAtual);
            }
        }

        public static void DeleteAllowedAreasByPerfil(int perfilId, Quasar_Entities db)
        {
            EnsurePerfilAreaAccessSchema(db);

            db.Database.ExecuteSqlCommand(
                "DELETE FROM dbo.PerfilAreaAcesso WHERE PerfilId = @p0",
                perfilId);
        }

        public static bool HasMenuAreaAccess(int perfilId, string area)
        {
            if (perfilId == 1 || string.IsNullOrWhiteSpace(area))
            {
                return true;
            }

            if (perfilId <= 0)
            {
                return false;
            }

            using (var db = new Quasar_Entities())
            {
                return GetAllowedAreasByPerfil(perfilId, db).Contains(area);
            }
        }

        public static List<MenuViewModel> GetMenusByPerfil(int perfilId, Quasar_Entities db, string area = null)
        {
            var menuIdsAutorizados = GetMenuIdsByPerfil(perfilId, db, area).ToHashSet();

            var menus = (from m in db.AppMenu
                         where m.Status == true
                            && (area == null || m.Area == area)
                         select new
                         {
                             m.Id,
                             m.Titulo,
                             m.Area,
                             m.Controller,
                             m.Action,
                             m.Css,
                             m.Status,
                             m.Nivel,
                             m.IdNivelSup,
                             m.Sequencia
                         }).ToList()
                         .Where(m => perfilId == 1 || menuIdsAutorizados.Contains(m.Id))
                         .Where(m => perfilId == 1 || !string.Equals(m.Controller, "Perfil", StringComparison.OrdinalIgnoreCase))
                         .ToList();

            var topLevelMenus = menus
                .Where(m => m.IdNivelSup == null)
                .OrderBy(m => m.Sequencia)
                .ThenBy(m => m.Id)
                .ToList();

            var rootMenuByArea = topLevelMenus
                .Where(m => !string.IsNullOrWhiteSpace(m.Area))
                .GroupBy(m => m.Area)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderBy(m => m.Sequencia).ThenBy(m => m.Id).Select(m => m.Id).First(),
                    StringComparer.OrdinalIgnoreCase);

            var itensMenu = topLevelMenus
                .Select(m =>
                {
                    var subMenus = menus
                        .Where(m2 =>
                            m2.IdNivelSup != null
                            && (perfilId == 1 || !string.Equals(m2.Controller, "Perfil", StringComparison.OrdinalIgnoreCase))
                            && (
                                m2.IdNivelSup == m.Id
                                || (
                                    m2.IdNivelSup != ConfiguracaoMenuId
                                    &&
                                    !string.IsNullOrWhiteSpace(m2.Area)
                                    && !menus.Any(parent => parent.Id == m2.IdNivelSup)
                                    && rootMenuByArea.ContainsKey(m2.Area)
                                    && rootMenuByArea[m2.Area] == m.Id
                                )
                            ))
                        .OrderBy(m2 => m2.Sequencia)
                        .ThenBy(m2 => m2.Id)
                        .Select(m2 => new SubMenu
                        {
                            Id = m2.Id,
                            Titulo = m2.Titulo,
                            Area = m2.Area,
                            Controller = m2.Controller,
                            Action = m2.Action,
                            Css = m2.Css,
                            Status = m2.Status,
                            IdNivelSup = m2.IdNivelSup,
                            Sequencia = m2.Sequencia
                        })
                        .ToList();

                    return new MenuViewModel
                    {
                        Id = m.Id,
                        Titulo = m.Titulo,
                        Area = m.Area,
                        Controller = m.Controller,
                        Action = m.Action,
                        Css = m.Css,
                        Status = m.Status,
                        Nivel = m.Nivel,
                        IdNivelSup = m.IdNivelSup,
                        Sequencia = m.Sequencia,
                        _menu = subMenus
                    };
                })
                .Where(m => string.IsNullOrWhiteSpace(m.Controller) || !string.IsNullOrWhiteSpace(m.Action) || m._menu.Count > 0)
                .ToList();

            return itensMenu;
        }

        public static bool HasFunctionAccess(string area, string controller, string action)
        {
            int perfilId = GetPerfilId();
            if (perfilId == 1)
            {
                return true;
            }

            if (controller == "Menu" || (controller == "Home" && action == "Index"))
            {
                return true;
            }

            if (perfilId == 0)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(area))
            {
                return true;
            }

            return HasMenuAreaAccess(perfilId, area);
        }

        private static void EnsurePerfilAreaAccessSchema(Quasar_Entities db)
        {
            if (PerfilAreaSchemaInitialized)
            {
                return;
            }

            lock (PerfilAreaSchemaLock)
            {
                if (PerfilAreaSchemaInitialized)
                {
                    return;
                }

                db.Database.ExecuteSqlCommand(@"
IF OBJECT_ID(N'dbo.PerfilAreaAcesso', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PerfilAreaAcesso
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PerfilAreaAcesso PRIMARY KEY,
        PerfilId INT NOT NULL,
        Area NVARCHAR(100) NOT NULL,
        Status BIT NOT NULL CONSTRAINT DF_PerfilAreaAcesso_Status DEFAULT(1),
        CriadoPor NVARCHAR(100) NULL,
        CriadoEm DATETIME NULL,
        ModificadoPor NVARCHAR(100) NULL,
        ModificadoEm DATETIME NULL,
        CONSTRAINT FK_PerfilAreaAcesso_PerfilUsuario FOREIGN KEY (PerfilId) REFERENCES dbo.PerfilUsuario(Id) ON DELETE CASCADE
    );

    CREATE UNIQUE NONCLUSTERED INDEX UX_PerfilAreaAcesso_PerfilId_Area
        ON dbo.PerfilAreaAcesso(PerfilId, Area);
END");

                SeedPerfilAreaAccessDefaults(db);
                PerfilAreaSchemaInitialized = true;
            }
        }

        private static void SeedPerfilAreaAccessDefaults(Quasar_Entities db)
        {
            int totalRegistros = db.Database
                .SqlQuery<int>("SELECT COUNT(1) FROM dbo.PerfilAreaAcesso")
                .FirstOrDefault();

            if (totalRegistros > 0)
            {
                return;
            }

            var perfis = db.PerfilUsuario
                .Select(p => p.Id)
                .ToList();

            var areasAtivas = GetActiveAreas(db);
            if (areasAtivas.Count == 0)
            {
                return;
            }

            var areasPadrao = areasAtivas
                .Where(a => !a.Equals("AdminApp", StringComparison.OrdinalIgnoreCase)
                    && !a.Equals("ConfiguracaoApp", StringComparison.OrdinalIgnoreCase)
                    && !a.Equals("ControleAcessoApp", StringComparison.OrdinalIgnoreCase))
                .ToList();

            string usuario = GetCurrentUser();
            if (string.IsNullOrWhiteSpace(usuario))
            {
                usuario = "SYSTEM";
            }

            DateTime dataAtual = GetCurrentDateTime();

            foreach (var perfilId in perfis)
            {
                IEnumerable<string> areasPerfil;

                if (perfilId == 1)
                {
                    areasPerfil = areasAtivas;
                }
                else if (perfilId == 8)
                {
                    areasPerfil = areasPadrao.Concat(new[] { "ControleAcessoApp" });
                }
                else
                {
                    areasPerfil = areasPadrao;
                }

                foreach (var area in areasPerfil.Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    InsertPerfilAreaAccessRow(db, perfilId, area, usuario, dataAtual);
                }
            }
        }

        private static void InsertPerfilAreaAccessRow(Quasar_Entities db, int perfilId, string area, string usuario, DateTime dataAtual)
        {
            db.Database.ExecuteSqlCommand(
                @"INSERT INTO dbo.PerfilAreaAcesso
                    (PerfilId, Area, Status, CriadoPor, CriadoEm, ModificadoPor, ModificadoEm)
                  VALUES
                    (@p0, @p1, 1, @p2, @p3, @p2, @p3)",
                perfilId,
                area,
                usuario,
                dataAtual);
        }

        private static List<string> GetActiveAreas(Quasar_Entities db)
        {
            return db.AppMenu
                .Where(m => m.Status == true && m.Area != null && m.Area != string.Empty)
                .Select(m => m.Area)
                .AsEnumerable()
                .Where(a => !IsIgnoredPerfilArea(a))
                .Distinct()
                .OrderBy(a => a)
                .ToList();
        }

        public static bool IsIgnoredPerfilArea(string area)
        {
            return string.Equals((area ?? string.Empty).Trim(), "AdminApp", StringComparison.OrdinalIgnoreCase);
        }

        public static string GetCrudPermission(string action, string codigo)
        {
            string value = (action ?? codigo ?? string.Empty).ToLowerInvariant();

            if (value.Contains("create") || value.Contains("add") || value.Contains("novo") || value.Contains("cadastrar"))
            {
                return "add";
            }

            if (value.Contains("edit") || value.Contains("update") || value.Contains("alterar") || value.Contains("atualizar"))
            {
                return "update";
            }

            if (value.Contains("delete") || value.Contains("remove") || value.Contains("excluir"))
            {
                return "delete";
            }

            if (value.Contains("index") || value.Contains("detail") || value.Contains("details") || value.Contains("view") || value.Contains("listar"))
            {
                return "view";
            }

            return "view";
        }

        public static int GetMenuCacheVersion(int perfilId)
        {
            string key = MenuCacheVersionKeyPrefix + perfilId;
            object value = HttpRuntime.Cache[key];

            if (value is int)
            {
                return (int)value;
            }

            lock (MenuCacheVersionLock)
            {
                value = HttpRuntime.Cache[key];
                if (value is int)
                {
                    return (int)value;
                }

                HttpRuntime.Cache.Insert(key, 1);
                return 1;
            }
        }

        public static void InvalidateMenuCache(int perfilId)
        {
            if (perfilId <= 0)
            {
                return;
            }

            string key = MenuCacheVersionKeyPrefix + perfilId;

            lock (MenuCacheVersionLock)
            {
                int version = GetMenuCacheVersion(perfilId);
                HttpRuntime.Cache.Insert(key, version + 1);
            }
        }
    }
}
