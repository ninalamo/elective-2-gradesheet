using elective_2_gradesheet.Data.Entities;

namespace elective_2_gradesheet.Services
{
    public interface ISectionService
    {
        Task<IEnumerable<Section>> GetAllSectionsAsync();
        Task<Section?> GetSectionByIdAsync(int id);
        Task<Section?> GetSectionWithDetailsAsync(int id);
        Task<(bool success, string message)> CreateSectionAsync(Section section);
        Task<(bool success, string message)> UpdateSectionAsync(Section section);
        Task<(bool success, string message)> DeleteSectionAsync(int id);
        Task<bool> SectionExistsAsync(int id);
        Task<bool> SectionNameExistsAsync(string name, string schoolYear, int? excludeId = null);
    }
}
