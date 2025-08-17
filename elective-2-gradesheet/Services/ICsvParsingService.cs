using CsvHelper;
using elective_2_gradesheet.Models;
using System.Globalization;

namespace elective_2_gradesheet.Services
{
    public interface ICsvParsingService
    {
        /// <summary>
        /// Parses the uploaded CSV file into a list of grade records.
        /// </summary>
        /// <param name="file">The uploaded file from the HTTP request.</param>
        /// <returns>A list of GradeRecordViewModel objects.</returns>
        IEnumerable<GradeRecordViewModel> ParseGradesCsv(IFormFile file);
    }

    public class CsvParsingService : ICsvParsingService
    {
        public IEnumerable<GradeRecordViewModel> ParseGradesCsv(IFormFile file)
        {
            // The 'using' statements ensure that the streams are properly disposed of.
            using (var reader = new StreamReader(file.OpenReadStream()))
            {


                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    // CsvHelper reads the records and maps them to the ViewModel.
                    // .ToList() is called to execute the read operation immediately.
                    return csv.GetRecords<GradeRecordViewModel>().ToList();
                }
            }
        }
    }
}
