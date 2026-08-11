using System;
using System.Collections.Generic;
using System.Linq;
using Simplify.Quasar.Models;

namespace Simplify.Quasar.Custom
{
    public sealed class LocacaoCreateRequest
    {
        public string Codigo { get; set; }
        public string Tipo { get; set; }
        public string Descricao { get; set; }
        public bool Bloqueado { get; set; }
        public int? AreaId { get; set; }
        public int? ZonaId { get; set; }
        public int? EquipamentoId { get; set; }
        public string Curva { get; set; }
        public string Estrategia { get; set; }
        public string Observacoes { get; set; }
    }

    public class LocacaoService
    {
        private readonly Quasar_Entities db;
        private readonly int filialId;
        private readonly string usuario;
        private readonly DateTime dataHora;

        public LocacaoService(Quasar_Entities db, int filialId, string usuario, DateTime dataHora)
        {
            this.db = db ?? throw new ArgumentNullException("db");
            this.filialId = filialId;
            this.usuario = usuario;
            this.dataHora = dataHora;
        }

        public Locacao Adicionar(LocacaoCreateRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            var codigo = NormalizarCodigo(request.Codigo);
            if (string.IsNullOrWhiteSpace(codigo))
            {
                throw new InvalidOperationException("O código da locação é obrigatório.");
            }

            if (codigo.Length > 100)
            {
                throw new InvalidOperationException("O código da locação não pode exceder 100 caracteres.");
            }

            var locacao = new Locacao
            {
                Codigo = codigo,
                Tipo = NormalizarTexto(request.Tipo, 100),
                Descricao = NormalizarTexto(request.Descricao, 100),
                Bloqueado = request.Bloqueado,
                AreaId = request.AreaId,
                ZonaId = request.ZonaId,
                EquipamentoId = request.EquipamentoId,
                Curva = NormalizarTexto(request.Curva, 100),
                Estrategia = NormalizarTexto(request.Estrategia, 100),
                Observacoes = NormalizarTexto(request.Observacoes, 500),
                CriadoPor = NormalizarTexto(usuario, 100),
                CriadoEm = dataHora,
                FilialId = filialId
            };

            db.Locacao.Add(locacao);
            return locacao;
        }

        public static string FormarCodigo(string zona, int corredor, int estante, string nivel, string compartimento)
        {
            return NormalizarCodigo(string.Join(" ", new[]
            {
                NormalizarSegmento(zona),
                corredor.ToString(),
                estante.ToString(),
                NormalizarSegmento(nivel),
                NormalizarSegmento(compartimento)
            }));
        }

        public static string NormalizarCodigo(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
            {
                return string.Empty;
            }

            return string.Join(" ", codigo
                .Trim()
                .ToUpperInvariant()
                .Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
        }

        public static string NormalizarSegmento(string valor)
        {
            return (valor ?? string.Empty).Trim().ToUpperInvariant();
        }

        private static string NormalizarTexto(string valor, int tamanhoMaximo)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                return null;
            }

            var normalizado = valor.Trim();
            return normalizado.Length <= tamanhoMaximo
                ? normalizado
                : normalizado.Substring(0, tamanhoMaximo);
        }
    }
}
