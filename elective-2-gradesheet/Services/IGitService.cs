using System.Threading.Tasks;

namespace elective_2_gradesheet.Services
{
    public interface IGitService
    {
        Task<GitCloneResult> CloneRepositoryAsync(string githubUrl, string outputDirectory = null);
        bool IsValidGithubUrl(string url);
        string GetDefaultCloneDirectory();
        bool DirectoryExists(string path);
        bool IsDirectoryEmpty(string path);
        string ValidateAndPrepareOutputDirectory(string outputDirectory, string repositoryName);
    }

    public class GitCloneResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string ClonedDirectory { get; set; }
        public string RepositoryName { get; set; }
        public Exception Exception { get; set; }
    }
}
