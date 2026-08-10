using Azure.Core;
using System.Text.RegularExpressions;

namespace QuasarApi.Helpers
{
    public class StringUtils
    {
        // Formatar locação conforme padrão Apollo
        public static string FormatarLocacao(string locacao)
        {
            string result = string.Empty;
            string d1 = string.Empty;
            string d2 = string.Empty;
            string d3 = string.Empty;

            string[] parts = locacao.Split(' ');
            if (parts.Length != 3)
            {
                return locacao;
            }

            try
            {
                d1 = parts[0].Substring(0, 2) + "." + parts[0].Substring(2, 1) + ".";
                d2 = parts[1] + ".";

                if (parts[2].Length == 3)
                {
                    d3 = parts[2].Substring(0, 2) + "." + parts[2].Substring(2, 1);
                }
                else
                {
                    d3 = parts[2].Substring(0, 2) + "." + parts[2].Substring(2, 2);
                }

                result = d1 + d2 + d3;
            }
            catch (Exception)
            {
                result = locacao;
            }

            return result;
        }

        public static string RemoverFormatacaoLocacao(string locacao)
        {
            string withSpaces = locacao.Replace('.', ' ');
            string pattern = @"([A-Za-z]) (\d)";
            string result = Regex.Replace(withSpaces, pattern, "$1$2");
            return result;
        }
    }

}
