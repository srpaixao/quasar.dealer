using Microsoft.EntityFrameworkCore;

using QuasarApi.DataBase;
using QuasarApi.DTO.Operations.Recebimento.Conferencia;
using QuasarApi.Services.Interfaces;

namespace QuasarApi.Services
{
    public class VolumeService : IVolumeService
    {
        private readonly AppDbContext _context;

        public VolumeService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<VolumeResumoDto>> ResumoVolumesAsync(int statusId, int areaId, int filialId)
        {
            var query = from v in _context.Volume
                        join a in _context.Area on v.AreaId equals a.Id
                        join sv in _context.StatusVolume on v.StatusId equals sv.Id
                        where a.Id == areaId
                           && a.FilialId == filialId
                           && v.FilialId == filialId
                        select new
                        {
                            v.VolumeNr,
                            v.NotaFiscalNr,
                            QtdeItens = v.QtdItens,
                            v.StatusId,
                            StatusNome = sv.Nome,
                            v.CriadoEm,
                            AreaId = a.Id
                        };

            var list = await query.ToListAsync();

            var result = list
                .GroupBy(v => v.VolumeNr)
                .Select(grp =>
                {
                    int statusAgrupado = grp.Any(x => x.StatusId == 3) ? 3
                        : grp.Any(x => x.StatusId == 1) ? 1
                        : grp.Any(x => x.StatusId == 2) ? 2
                        : grp.Min(x => x.StatusId);

                    return new VolumeResumoDto
                    {
                        VolumeNr = grp.Key,
                        NotaFiscalNr = string.Join(" / ", grp.Select(x => x.NotaFiscalNr).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct()),
                        QtdeItens = grp.Sum(x => x.QtdeItens),
                        StatusId = statusAgrupado,
                        StatusNome = grp.Where(x => x.StatusId == statusAgrupado).Select(x => x.StatusNome).FirstOrDefault() ?? string.Empty,
                        CriadoEm = grp.Max(x => x.CriadoEm)
                    };
                })
                .Where(x => statusId == 0 || x.StatusId == statusId)
                .OrderByDescending(x => x.CriadoEm)
                .ThenBy(x => x.VolumeNr)
                .ToList();

            List<string> volumesResultado = result
                .Select(x => x.VolumeNr ?? string.Empty)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            var notasPorVolume = await (from item in _context.NotaFiscalItem.AsNoTracking()
                                        join nota in _context.NotaFiscal.AsNoTracking() on item.NotaFiscalId equals nota.Id
                                        where item.FilialId == filialId
                                           && nota.FilialId == filialId
                                           && item.Volume != null
                                           && volumesResultado.Contains(item.Volume!.Trim())
                                        select new { VolumeNr = item.Volume!.Trim(), nota.Numero })
                .Distinct()
                .ToListAsync();

            var notasLookup = notasPorVolume
                .GroupBy(x => x.VolumeNr)
                .ToDictionary(x => x.Key, x => string.Join(" / ", x.Select(n => n.Numero).Distinct()));

            foreach (VolumeResumoDto volume in result)
            {
                string volumeNr = volume.VolumeNr ?? string.Empty;
                volume.NotaFiscalNr = notasLookup.TryGetValue(volumeNr, out string? notas)
                    ? notas
                    : volume.NotaFiscalNr;
            }

            return result;
        }
    }

}
