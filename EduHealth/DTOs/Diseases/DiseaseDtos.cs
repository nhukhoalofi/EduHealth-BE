namespace EduHealth.DTOs.Diseases
{
    public class DiseaseListItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
    }

    public class CreateDiseaseRequestDto
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
    }

    public class DiseaseDetailDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
    }
}
