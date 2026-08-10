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

        public async Task<List<VolumeResumoDto>> ResumoVolumesAsync(int statusId, int areaId, int? filialId)
        {
            var query = from v in _context.Volume
                        join a in _context.Area on v.AreaId equals a.Id
                        join sv in _context.StatusVolume on v.StatusId equals sv.Id
                        where a.Id == areaId &&
                              v.FilialId == filialId
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

            if (statusId != 0)
            {
                list = list.Where(x => x.StatusId == statusId).ToList();
            }

            var result = list
                .GroupBy(v => v.VolumeNr)
                .Select(grp => new VolumeResumoDto
                {
                    VolumeNr = grp.Key,
                    NotaFiscalNr = string.Join(" / ", grp.Select(x => x.NotaFiscalNr)),
                    QtdeItens = grp.Sum(x => x.QtdeItens),
                    StatusId = grp.First().StatusId,
                    StatusNome = grp.First().StatusNome,
                    CriadoEm = grp.Max(x => x.CriadoEm)
                })
                .ToList();

            return result;
        }
    }

}
