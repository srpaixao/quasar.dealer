using Microsoft.EntityFrameworkCore;
using QuasarApi.Database.Models;
using QuasarApi.DataBase;
using QuasarApi.DTO.Operations.Recebimento.Conferencia;
using QuasarApi.Services.Interfaces;
using QuasarApi.Helpers;

namespace QuasarApi.Services
{
    public class ConferenciaVolumeService : IConferenciaVolumeService
    {
        private readonly AppDbContext _context;

        public ConferenciaVolumeService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<UpdateVolumeResponseDto> UpdateVolumeAsync(UpdateVolumeRequestDto request)
        {
            var response = new UpdateVolumeResponseDto();

            var volume = await _context.Volume
                .Where(x => x.AreaId == request.Area &&
                            x.VolumeNr == request.Volume &&
                            x.StatusId != 3 &&
                            x.FilialId == request.FilialId)
                .FirstOrDefaultAsync();

            if (volume == null)
            {
                _context.Volume.Add(new Volume
                {
                    NotaFiscalNr = string.Empty,
                    VolumeNr = request.Volume,
                    StatusId = 3,
                    AreaId = request.Area,
                    QtdItens = 0,
                    Imprimir = false,
                    Danfe = string.Empty,
                    CriadoPor = request.Usuario,
                    CriadoEm = CurrentDateTime.GetCurrentDateTime(),
                    FilialId = request.FilialId
                });

                await _context.SaveChangesAsync();

                return await GerarResumo("Volume incorreto!", true, true, request.Area, request.FilialId);
            }

            using var tr = await _context.Database.BeginTransactionAsync();

            try
            {
                volume.StatusId = 2;
                // await _context.SaveChangesAsync();

                 bool imprimirEtiqueta = await _context.Area
                     .Where(a => a.Id == request.Area && a.FilialId == request.FilialId)
                     .Select(a => a.Etiqueta ?? false)
                     .FirstOrDefaultAsync();

                int StatusId = 4;
                if (imprimirEtiqueta)
                    StatusId = 3;

                var itensNotaFiscal = await _context.NotaFiscalItem
                    .Where(nfi => nfi.Volume == request.Volume && nfi.FilialId == request.FilialId)
                    .ToListAsync();
                
                foreach (var item in itensNotaFiscal)
                {
                    item.StatusId = StatusId;
                }
                
                await _context.SaveChangesAsync();

                int idNF = await _context.Volume
                    .Where(x => x.VolumeNr == request.Volume && x.FilialId == request.FilialId)
                    .Join(
                        _context.NotaFiscal.Where(nf => nf.FilialId == request.FilialId),
                        v => v.NotaFiscalNr,
                        nf => nf.Numero,
                        (v, nf) => nf.Id)
                    .Distinct()
                    .FirstOrDefaultAsync();

                if (idNF > 0)
                {
                    var pendentes = await _context.NotaFiscalItem
                        .Where(x => x.StatusId == 2 && x.NotaFiscalId == idNF && x.FilialId == request.FilialId)
                        .Select(x => x.Volume)
                        .Distinct()
                        .CountAsync();

                    if (pendentes == 0)
                    {
                        var notaFiscal = await _context.NotaFiscal
                            .FirstOrDefaultAsync(nf => nf.Id == idNF && nf.FilialId == request.FilialId);
                        if (notaFiscal != null)
                        {
                            notaFiscal.StatusId = StatusId;
                            await _context.SaveChangesAsync();
                        }
                    }

                else
                    {
                        var notaFiscal = await _context.NotaFiscal
                            .FirstOrDefaultAsync(nf => nf.Id == idNF && nf.FilialId == request.FilialId);
                        //if (notaFiscal != null && notaFiscal.StatusId == 2)
                        //{
                            notaFiscal.StatusId = 3;
                            await _context.SaveChangesAsync();
                        //}
                    }
                }
            
                await tr.CommitAsync();
            }
            catch (Exception ex)
            {
                await tr.RollbackAsync();
                return await GerarResumo(ex.Message, true, false, request.Area, request.FilialId);
            }

            var pendentesFinal = await _context.Volume
                .Where(x => x.StatusId == 1 && x.AreaId == request.Area && x.FilialId == request.FilialId)
                .Select(x => x.VolumeNr)
                .Distinct()
                .CountAsync();

            if (pendentesFinal == 0)
                return await GerarResumo("Conferência Finalizada!", false, false, request.Area, request.FilialId, true);
            else
                return await GerarResumo("Operação executada com sucesso", false, false, request.Area, request.FilialId);
        }

        private async Task<UpdateVolumeResponseDto> GerarResumo(
            string msg,
            bool erro,
            bool notfound,
            int areaId,
            int? filialId,
            bool finalizado = false)
        {
            var volumes = _context.Volume.Where(x => x.AreaId == areaId && x.FilialId == filialId);
            return new UpdateVolumeResponseDto
            {
                Msg = msg,
                Erro = erro,
                NotFound = notfound,
                Finalizado = finalizado,
                Total = await volumes.Where(x => x.StatusId != 3).Select(x => x.VolumeNr).Distinct().CountAsync(),
                Pendentes = await volumes.Where(x => x.StatusId == 1).Select(x => x.VolumeNr).Distinct().CountAsync(),
                Conferidos = await volumes.Where(x => x.StatusId == 2).Select(x => x.VolumeNr).Distinct().CountAsync(),
                Incorretos = await volumes.Where(x => x.StatusId == 3).Select(x => x.VolumeNr).Distinct().CountAsync(),
            };
        }
    }
}
