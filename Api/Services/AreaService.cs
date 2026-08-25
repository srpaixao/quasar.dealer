using Microsoft.EntityFrameworkCore;

using QuasarApi.Database.Models;
using QuasarApi.DataBase;
using QuasarApi.Services.Interfaces;
using static QuasarApi.DTO.Management.AreaDTO;

namespace QuasarApi.Services
{
    public class AreaService : IAreaService
    {
        private readonly AppDbContext _context;

        public AreaService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<AreaReadDto>> ObterTodosAsync()
        {
            return await _context.Area
                .Select(x => new AreaReadDto
                {
                    Id = x.Id,
                    Nome = x.Nome,
                    Descricao = string.IsNullOrWhiteSpace(x.Descricao) ? x.Nome : x.Descricao
                }).ToListAsync();
        }

        public async Task<AreaReadDto?> ObterPorIdAsync(int id)
        {
            return await _context.Area
                .Where(x => x.Id == id)
                .Select(x => new AreaReadDto
                {
                    Id = x.Id,
                    Nome = x.Nome,
                    Descricao = string.IsNullOrWhiteSpace(x.Descricao) ? x.Nome : x.Descricao
                }).FirstOrDefaultAsync();
        }

        public async Task<AreaReadDto> CriarAsync(AreaCreateDto dto)
        {
            var area = new Area
            {
                Nome = dto.Nome,
                Descricao = dto.Descricao,
               // CriadoEm = DateTime.UtcNow
            };

            _context.Area.Add(area);
            await _context.SaveChangesAsync();

            return new AreaReadDto
            {
                Id = area.Id,
                Nome = area.Nome,
                Descricao = area.Descricao
            };
        }

        public async Task AtualizarAsync(AreaUpdateDto dto)
        {
            var area = await _context.Area.FindAsync(dto.Id);
            if (area is null)
            {
                throw new KeyNotFoundException("Área não encontrada");
            }

            area.Nome = dto.Nome;
            area.Descricao = dto.Descricao;
            await _context.SaveChangesAsync();
        }

        public async Task ExcluirAsync(int id)
        {
            var area = await _context.Area.FindAsync(id);
            if (area is null)
            {
                throw new KeyNotFoundException("Usuário não encontrado");
            }

            _context.Area.Remove(area);
            await _context.SaveChangesAsync();
        }
    }
}
