using Microsoft.EntityFrameworkCore;
using QuasarApi.Database.Models;
using QuasarApi.DataBase;
using QuasarApi.DTO.Operations.Recebimento.Conferencia;
using QuasarApi.Helpers;
using QuasarApi.Services.Interfaces;

namespace QuasarApi.Services
{
    public class ConferenciaVolumeService : IConferenciaVolumeService
    {
        private readonly AppDbContext _context;

        public ConferenciaVolumeService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<UpdateVolumeResponseDto> UpdateVolumeAsync(UpdateVolumeRequestDto request, int filialId, string usuario)
        {
            string volumeNr = (request.Volume ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(volumeNr))
                return CriarErro("Volume nao informado.");

            bool areaValida = await _context.Area.AsNoTracking()
                .AnyAsync(x => x.Id == request.Area && x.FilialId == filialId);
            if (!areaValida)
                return CriarErro("Area de recebimento invalida para a filial do usuario.");

            List<Volume> registrosVolume = await _context.Volume
                .Where(x => x.AreaId == request.Area
                    && x.FilialId == filialId
                    && x.VolumeNr == volumeNr
                    && x.StatusId != 3)
                .ToListAsync();

            if (registrosVolume.Count == 0)
            {
                bool incorretoJaRegistrado = await _context.Volume.AnyAsync(x =>
                    x.AreaId == request.Area
                    && x.FilialId == filialId
                    && x.VolumeNr == volumeNr
                    && x.StatusId == 3);

                if (!incorretoJaRegistrado)
                {
                    _context.Volume.Add(new Volume
                    {
                        NotaFiscalNr = string.Empty,
                        VolumeNr = volumeNr,
                        StatusId = 3,
                        QtdItens = 0,
                        AreaId = request.Area,
                        Imprimir = false,
                        Danfe = string.Empty,
                        FilialId = filialId,
                        CriadoPor = usuario,
                        CriadoEm = CurrentDateTime.GetCurrentDateTime()
                    });

                    await _context.SaveChangesAsync();
                }

                return await GerarResumo("Volume incorreto!", true, true, request.Area, filialId);
            }

            await using var tr = await _context.Database.BeginTransactionAsync();
            try
            {
                DateTime agora = CurrentDateTime.GetCurrentDateTime();
                foreach (Volume registro in registrosVolume)
                {
                    registro.StatusId = 2;
                    registro.ModificadoPor = usuario;
                    registro.ModificadoEm = agora;
                }

                int statusArea = (await _context.Area.FindAsync(request.Area))?.Etiqueta == true ? 3 : 7;
                List<NotaFiscalItem> itensNotaFiscal = await _context.NotaFiscalItem
                    .Where(x => x.FilialId == filialId && x.Volume != null && x.Volume.Trim() == volumeNr)
                    .ToListAsync();

                foreach (NotaFiscalItem item in itensNotaFiscal)
                    item.StatusId = statusArea;

                List<int> notasIds = itensNotaFiscal.Select(x => x.NotaFiscalId).Distinct().ToList();
                foreach (int notaId in notasIds)
                {
                    bool possuiVolumePendente = await _context.NotaFiscalItem.AnyAsync(x =>
                        x.NotaFiscalId == notaId
                        && x.FilialId == filialId
                        && x.StatusId < 4);

                    if (!possuiVolumePendente)
                    {
                        NotaFiscal? notaFiscal = await _context.NotaFiscal
                            .FirstOrDefaultAsync(x => x.Id == notaId && x.FilialId == filialId);
                        if (notaFiscal != null)
                            notaFiscal.StatusId = 7;
                    }
                }

                await _context.SaveChangesAsync();
                await tr.CommitAsync();
            }
            catch (Exception ex)
            {
                await tr.RollbackAsync();
                return await GerarResumo(ex.Message, true, false, request.Area, filialId);
            }

            UpdateVolumeResponseDto resumo = await GerarResumo(
                "Operacao executada com sucesso",
                false,
                false,
                request.Area,
                filialId);

            if (resumo.Pendentes == 0)
            {
                resumo.Msg = "Conferencia finalizada!";
                resumo.Finalizado = true;
            }

            return resumo;
        }

        private async Task<UpdateVolumeResponseDto> GerarResumo(
            string msg,
            bool erro,
            bool notfound,
            int areaId,
            int filialId,
            bool finalizado = false)
        {
            var registros = await _context.Volume.AsNoTracking()
                .Where(x => x.AreaId == areaId && x.FilialId == filialId)
                .Select(x => new { x.VolumeNr, x.StatusId })
                .ToListAsync();

            var volumes = registros
                .GroupBy(x => x.VolumeNr)
                .Select(grupo => grupo.Any(x => x.StatusId == 3) ? 3
                    : grupo.Any(x => x.StatusId == 1) ? 1
                    : grupo.Any(x => x.StatusId == 2) ? 2
                    : grupo.Min(x => x.StatusId))
                .ToList();

            return new UpdateVolumeResponseDto
            {
                Msg = msg,
                Erro = erro,
                NotFound = notfound,
                Finalizado = finalizado,
                Total = volumes.Count(x => x != 3),
                Pendentes = volumes.Count(x => x == 1),
                Conferidos = volumes.Count(x => x == 2),
                Incorretos = volumes.Count(x => x == 3)
            };
        }

        private static UpdateVolumeResponseDto CriarErro(string mensagem)
        {
            return new UpdateVolumeResponseDto
            {
                Msg = mensagem,
                Erro = true
            };
        }
    }
}
