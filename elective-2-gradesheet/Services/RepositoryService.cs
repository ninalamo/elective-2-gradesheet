using elective_2_gradesheet.Models;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace elective_2_gradesheet.Services;

public class RepositoryService
{
    private readonly IGitService _gitService;

    public RepositoryService(IGitService gitService)
    {
        _gitService = gitService;
    }

    public async Task<RepositoryCloneResult> CloneRepositoryAsync(string repositoryUrl, int studentId)
    {
        try
        {
            if (string.IsNullOrEmpty(repositoryUrl))
            {
                return new RepositoryCloneResult
                {
                    Success = false,
                    ErrorMessage = "Repository URL is empty or null"
                };
            }

            // Create a unique output directory for this student and timestamp
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var outputDirectory = Path.Combine(Path.GetTempPath(), "bulk_grading", $"student_{studentId}_{timestamp}");

            var result = await _gitService.CloneRepositoryAsync(repositoryUrl, outputDirectory);

            return new RepositoryCloneResult
            {
                Success = result.Success,
                ClonedDirectory = result.ClonedDirectory,
                ErrorMessage = result.Success ? null : result.Message
            };
        }
        catch (Exception ex)
        {
            return new RepositoryCloneResult
            {
                Success = false,
                ErrorMessage = $"Exception during repository clone: {ex.Message}"
            };
        }
    }

    public async Task<RepositoryScanResult> ScanRepositoryAsync(string clonedDirectory)
    {
        try
        {
            if (!Directory.Exists(clonedDirectory))
            {
                return new RepositoryScanResult
                {
                    Success = false,
                    ErrorMessage = "Cloned directory does not exist"
                };
            }

            var scannedFiles = new List<FileContent>();
            
            // Find project directories (containing .csproj files)
            var projectDirectories = FindProjectDirectories(clonedDirectory);
            
            if (!projectDirectories.Any())
            {
                // If no project directories found, scan the entire directory
                projectDirectories.Add(clonedDirectory);
            }

            // Scan each project directory
            foreach (var projectDir in projectDirectories)
            {
                var projectFiles = Directory.GetFiles(projectDir, "*", SearchOption.AllDirectories)
                    .Where(f => !Path.GetFileName(f).StartsWith(".")) // Skip hidden files
                    .Where(f => !f.Contains("bin", StringComparison.OrdinalIgnoreCase)) // Skip bin folders
                    .Where(f => !f.Contains("obj", StringComparison.OrdinalIgnoreCase)) // Skip obj folders
                    .ToArray();

                // Read all files from this project
                foreach (var filePath in projectFiles)
                {
                    try
                    {
                        var relativePath = Path.GetRelativePath(projectDir, filePath).Replace("\\", "/");
                        var content = await File.ReadAllTextAsync(filePath);
                        
                        scannedFiles.Add(new FileContent
                        {
                            Name = Path.GetFileName(filePath),
                            Path = relativePath,
                            Content = content,
                            ProjectDirectory = Path.GetRelativePath(clonedDirectory, projectDir)
                        });
                    }
                    catch (Exception)
                    {
                        // Skip binary files or files that can't be read as text
                        continue;
                    }
                }
            }

            return new RepositoryScanResult
            {
                Success = true,
                ScannedFiles = scannedFiles
            };
        }
        catch (Exception ex)
        {
            return new RepositoryScanResult
            {
                Success = false,
                ErrorMessage = $"Exception during repository scan: {ex.Message}"
            };
        }
    }

    private List<string> FindProjectDirectories(string rootDirectory)
    {
        var projectDirectories = new List<string>();
        
        try
        {
            // Look for .csproj files
            var csprojFiles = Directory.GetFiles(rootDirectory, "*.csproj", SearchOption.AllDirectories);
            
            foreach (var csprojFile in csprojFiles)
            {
                var directory = Path.GetDirectoryName(csprojFile);
                if (directory != null && !projectDirectories.Contains(directory))
                {
                    projectDirectories.Add(directory);
                }
            }
        }
        catch (Exception)
        {
            // If there's an error finding project files, return empty list
            // The calling method will fall back to scanning the root directory
        }

        return projectDirectories;
    }
}

