using static QuasarApi.DTO.Management.AreaDTO;

namespace QuasarApi.Services.Interfaces
{
    public interface IAreaService
    {
        Task<IEnumerable<AreaReadDto>> ObterTodosAsync();
        Task<AreaReadDto?> ObterPorIdAsync(int id);
        Task<AreaReadDto> CriarAsync(AreaCreateDto dto);
        Task AtualizarAsync(AreaUpdateDto dto);
        Task ExcluirAsync(int id);

    }
}
