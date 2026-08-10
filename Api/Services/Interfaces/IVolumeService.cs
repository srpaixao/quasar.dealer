using QuasarApi.DTO.Operations.Recebimento.Conferencia;

namespace QuasarApi.Services.Interfaces
{
    public interface IVolumeService
    {
        Task<List<VolumeResumoDto>> ResumoVolumesAsync(int statusId, int areaId, int? filialId);
    }
}
