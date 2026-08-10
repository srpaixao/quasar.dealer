using static QuasarApi.DTO.Management.AreaDTO;

namespace QuasarApi.Services.Interfaces
{
    public interface IAreaService
    {
        Task<IEnumerable<AreaReadDto>> ObterTodosAsync(int? filialId);
        Task<AreaReadDto?> ObterPorIdAsync(int id, int? filialId);
        Task<AreaReadDto> CriarAsync(AreaCreateDto dto);
        Task AtualizarAsync(AreaUpdateDto dto, int? filialId);
        Task ExcluirAsync(int id, int? filialId);

    }
}
