namespace QuasarApi.DTO.Management
{
    public class AreaDTO
    {
        public class AreaCreateDto
        {
            public string Nome { get; set; } = null!;
            public string Descricao { get; set; } = null!;
        }

        public class AreaUpdateDto
        {
            public int Id { get; set; }
            public string Nome { get; set; } = null!;
            public string Descricao { get; set; } = null!;
        }

        public class AreaReadDto
        {
            public int Id { get; set; }
            public string Nome { get; set; } = null!;
            public string Descricao { get; set; } = null!;
            public string Tipo { get; set; } = null!;
        }
    }
}
