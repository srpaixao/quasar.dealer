using QuasarApi.DTO.Operations.Recebimento.Conferencia;

namespace QuasarApi.Services.Interfaces
{
    public interface IConferenciaVolumeService
    {
        Task<UpdateVolumeResponseDto> UpdateVolumeAsync(UpdateVolumeRequestDto request, int filialId, string usuario);
    }
}
