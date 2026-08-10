using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using Microsoft.Ajax.Utilities;
using Simplify.Quasar.Areas.EstoqueApp.ViewModels;
using Simplify.Quasar.Models;

namespace Simplify.Quasar.Custom
{
    public class Util
    {
        public static bool IsTestEnvironment()
        {
            var environment = ConfigurationManager.AppSettings["Environment"];
            return environment != null && environment.ToLower() == "test";
        }

        public static string GetSessionCulture()
        {
            CultureInfo currentCulture = Thread.CurrentThread.CurrentCulture;

            if (HttpContext.Current == null || HttpContext.Current.Session == null || HttpContext.Current.Session["lang"] == null)
            {
                HttpContext.Current.Session["lang"] = currentCulture;
            }

            string culture = HttpContext.Current.Session["lang"].ToString();

            return culture;
        }

        public static string GetCurrentUser()
        {
            try
            {
                return HttpContext.Current.Session["useraccount"].ToString();
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        public static int GetCurrentFilial()
        {
            int filialid = 0;
            try
            {
                filialid = int.Parse(HttpContext.Current.Session["filialid"].ToString());
            }
            catch (Exception)
            {
                filialid = 0;
            }

            return filialid;
        }

        public static int GetPerfilId()
        {
            int perfilid = 0;
            try
            {
                perfilid = int.Parse(HttpContext.Current.Session["perfilid"].ToString());
            }
            catch (Exception)
            {
                perfilid = 0;
            }

            return perfilid;
        }

        public static int GetPeriodoRecebimento()
        {
            using (var db = new Quasar_Entities())
            {
                return db.AppConfig
                    .Where(m => m.Nome == "PeriodoRecebimento")
                    .Select(m => m.Valor)
                    .AsEnumerable()
                    .Select(v => int.TryParse(v, out var x) ? x : (int?)null)
                    .FirstOrDefault() ?? 30;
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

        // Lista permissões de um determinado perfil para um componente
        public static string GetPermissoes(string componente)
        {
            return "[add][update][delete][view]";
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
                       //where item.FilialId == filial
                       select new SelectListItem
                       {
                           Value = item.Id.ToString(),
                           Text = item.Nome,
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
                           Text = item.Nome,
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
        public static IEnumerable<SelectListItem> GetPerfisUsuario(int? id)
        {
            Quasar_Entities db = new Quasar_Entities();

            var ddl = (from item in db.PerfilUsuario
                       select new SelectListItem
                       {
                           Value = item.Id.ToString(),
                           Text = item.Nome,
                           Selected = item.Id == id
                       }).ToList();

            db.Dispose();

            return ddl.OrderBy(x => x.Text);
        }

        public static IEnumerable<SelectListItem> GetTransportadoraDDL(int? id)
        {
            Quasar_Entities db = new Quasar_Entities();

            var ddl = (from item in db.Transportadora
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
            sql = sql.Replace("@data_sistema", GetCurrentDateTime().ToString("yyyy-MM-dd HH:mm:ss"));
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
    }
}