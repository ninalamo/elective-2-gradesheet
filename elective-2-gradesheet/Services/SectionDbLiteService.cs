using elective_2_gradesheet.Data;
using elective_2_gradesheet.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace elective_2_gradesheet.Services
{
    public class SectionDbLiteService : ISectionService
    {
        private readonly ApplicationDbLiteContext _context;

        public SectionDbLiteService(ApplicationDbLiteContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Section>> GetAllSectionsAsync()
        {
            return await _context.Sections
                .Include(s => s.Students)
                .Include(s => s.ActivityTemplates)
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        public async Task<Section?> GetSectionByIdAsync(int id)
        {
            return await _context.Sections.FindAsync(id);
        }

        public async Task<Section?> GetSectionWithDetailsAsync(int id)
        {
            return await _context.Sections
                .Include(s => s.Students)
                .Include(s => s.ActivityTemplates)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<(bool success, string message)> CreateSectionAsync(Section section)
        {
            try
            {
                // Check if section name already exists for the same school year
                var existingSection = await _context.Sections
                    .FirstOrDefaultAsync(s => s.Name == section.Name && s.SchoolYear == section.SchoolYear);
                
                if (existingSection != null)
                {
                    return (false, "A section with this name already exists for the selected school year.");
                }

                _context.Add(section);
                await _context.SaveChangesAsync();
                return (true, $"Section '{section.Name}' created successfully!");
            }
            catch (Exception ex)
            {
                return (false, $"Error creating section: {ex.Message}");
            }
        }

        public async Task<(bool success, string message)> UpdateSectionAsync(Section section)
        {
            try
            {
                // Check if section name already exists for the same school year (excluding current section)
                var existingSection = await _context.Sections
                    .FirstOrDefaultAsync(s => s.Name == section.Name && s.SchoolYear == section.SchoolYear && s.Id != section.Id);
                
                if (existingSection != null)
                {
                    return (false, "A section with this name already exists for the selected school year.");
                }

                _context.Update(section);
                await _context.SaveChangesAsync();
                return (true, $"Section '{section.Name}' updated successfully!");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await SectionExistsAsync(section.Id))
                {
                    return (false, "Section not found.");
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                return (false, $"Error updating section: {ex.Message}");
            }
        }

        public async Task<(bool success, string message)> DeleteSectionAsync(int id)
        {
            try
            {
                var section = await _context.Sections
                    .Include(s => s.Students)
                    .Include(s => s.ActivityTemplates)
                    .FirstOrDefaultAsync(s => s.Id == id);
                    
                if (section == null)
                {
                    return (false, "Section not found.");
                }

                // Check if section has students or activity templates
                if (section.Students.Any() || section.ActivityTemplates.Any())
                {
                    return (false, "Cannot delete section that contains students or activity templates. Please move or delete them first.");
                }

                _context.Sections.Remove(section);
                await _context.SaveChangesAsync();
                return (true, $"Section '{section.Name}' deleted successfully!");
            }
            catch (Exception ex)
            {
                return (false, $"Error deleting section: {ex.Message}");
            }
        }

        public async Task<bool> SectionExistsAsync(int id)
        {
            return await _context.Sections.AnyAsync(e => e.Id == id);
        }

        public async Task<bool> SectionNameExistsAsync(string name, string schoolYear, int? excludeId = null)
        {
            var query = _context.Sections.Where(s => s.Name == name && s.SchoolYear == schoolYear);
            
            if (excludeId.HasValue)
            {
                query = query.Where(s => s.Id != excludeId.Value);
            }
            
            return await query.AnyAsync();
        }
    }
}
