using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace elective_2_gradesheet.Services
{
    public class GitService : IGitService
    {
        public async Task<GitCloneResult> CloneRepositoryAsync(string githubUrl, string outputDirectory = null)
        {
            try
            {
                if (!IsValidGithubUrl(githubUrl))
                {
                    return new GitCloneResult
                    {
                        Success = false,
                        Message = "Invalid GitHub URL. Expected https://github.com/<owner>/<repo>[.git] or git@github.com:<owner>/<repo>.git"
                    };
                }

                var repoName = GetRepoNameFromUrl(githubUrl);
                var targetPath = ValidateAndPrepareOutputDirectory(outputDirectory, repoName);
                
                // Ensure the parent directory exists
                var parentDir = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(parentDir))
                {
                    Directory.CreateDirectory(parentDir);
                }

                // If directory exists and is not empty, delete it to ensure fresh clone
                if (Directory.Exists(targetPath))
                {
                    if (!IsDirectoryEmpty(targetPath))
                    {
                        try
                        {
                            ForceDeleteDirectory(targetPath);
                        }
                        catch (Exception ex)
                        {
                            return new GitCloneResult
                            {
                                Success = false,
                                Message = $"Failed to delete existing directory '{targetPath}': {ex.Message}",
                                ClonedDirectory = targetPath,
                                RepositoryName = repoName
                            };
                        }
                    }
                }
                else
                {
                    Directory.CreateDirectory(targetPath);
                }

                var psi = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = $"clone --depth 1 \"{githubUrl}\" \"{targetPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = psi };
                var stdout = new StringBuilder();
                var stderr = new StringBuilder();

                process.OutputDataReceived += (_, e) => { if (e.Data != null) stdout.AppendLine(e.Data); };
                process.ErrorDataReceived += (_, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };

                try
                {
                    process.Start();
                }
                catch (Exception ex)
                {
                    return new GitCloneResult
                    {
                        Success = false,
                        Message = "Failed to start git process. Ensure Git is installed and available in PATH.",
                        Exception = ex
                    };
                }

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                await process.WaitForExitAsync();

                if (process.ExitCode == 0)
                {
                    return new GitCloneResult
                    {
                        Success = true,
                        Message = stdout.ToString(),
                        ClonedDirectory = targetPath,
                        RepositoryName = repoName
                    };
                }
                else
                {
                    return new GitCloneResult
                    {
                        Success = false,
                        Message = $"git clone failed: {stderr}",
                        ClonedDirectory = targetPath,
                        RepositoryName = repoName
                    };
                }
            }
            catch (Exception ex)
            {
                return new GitCloneResult
                {
                    Success = false,
                    Message = ex.Message,
                    Exception = ex
                };
            }
        }

        public bool IsValidGithubUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;

            // HTTPS form: https://github.com/owner/repo(.git)
            var https = Regex.IsMatch(url, @"^https://(www\.)?github\.com/[^/\s]+/[^/\s]+(\.git)?/?$", RegexOptions.IgnoreCase);
            // SSH form: git@github.com:owner/repo(.git)
            var ssh = Regex.IsMatch(url, @"^git@github\.com:[^/\s]+/[^/\s]+(\.git)?$", RegexOptions.IgnoreCase);
            return https || ssh;
        }

        public string GetDefaultCloneDirectory()
        {
            try
            {
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var dir = Path.Combine(home, "GradesheetClones");
                return dir;
            }
            catch
            {
                return Path.Combine(Path.GetTempPath(), "GradesheetClones");
            }
        }

        public bool DirectoryExists(string path) => Directory.Exists(path);

        public bool IsDirectoryEmpty(string path)
        {
            if (!Directory.Exists(path)) return true;
            return !Directory.EnumerateFileSystemEntries(path).Any();
        }

        public string ValidateAndPrepareOutputDirectory(string outputDirectory, string repositoryName)
        {
            string finalDirectory;
            
            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                // Use default directory
                finalDirectory = Path.Combine(GetDefaultCloneDirectory(), repositoryName);
            }
            else
            {
                // Use custom directory
                finalDirectory = Path.IsPathRooted(outputDirectory) 
                    ? outputDirectory 
                    : Path.GetFullPath(outputDirectory);
                    
                // If the custom path doesn't end with repo name, append it
                if (!Path.GetFileName(finalDirectory).Equals(repositoryName, StringComparison.OrdinalIgnoreCase))
                {
                    finalDirectory = Path.Combine(finalDirectory, repositoryName);
                }
            }
            
            return finalDirectory;
        }

        /// <summary>
        /// Force delete a directory by removing readonly attributes and handling locked files
        /// </summary>
        private static void ForceDeleteDirectory(string path)
        {
            if (!Directory.Exists(path))
                return;

            // First attempt: try normal deletion
            try
            {
                Directory.Delete(path, true);
                return;
            }
            catch (UnauthorizedAccessException)
            {
                // Continue to force deletion
            }
            catch (IOException)
            {
                // Continue to force deletion
            }

            // Second attempt: Remove readonly attributes from all files and directories
            try
            {
                RemoveReadOnlyAttribute(new DirectoryInfo(path));
                Directory.Delete(path, true);
                return;
            }
            catch (Exception)
            {
                // Continue to final attempt
            }

            // Final attempt: Use robocopy to delete (Windows-specific)
            try
            {
                // Create a temporary empty directory
                var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                Directory.CreateDirectory(tempDir);

                // Use robocopy to "mirror" empty directory (effectively deleting target)
                var psi = new ProcessStartInfo
                {
                    FileName = "robocopy",
                    Arguments = $"\"{tempDir}\" \"{path}\" /MIR /NFL /NDL /NJH /NJS /NC /NS /NP",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using var process = Process.Start(psi);
                process.WaitForExit();

                // Clean up temp directory
                Directory.Delete(tempDir);
                
                // Try to delete the now-empty target directory
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
            }
            catch (Exception)
            {
                // If all else fails, throw the original exception
                throw new IOException($"Unable to delete directory '{path}'. Please ensure no files are in use and you have sufficient permissions.");
            }
        }

        /// <summary>
        /// Recursively remove readonly attributes from files and directories
        /// </summary>
        private static void RemoveReadOnlyAttribute(DirectoryInfo directory)
        {
            try
            {
                // Remove readonly from the directory itself
                if (directory.Attributes.HasFlag(FileAttributes.ReadOnly))
                {
                    directory.Attributes &= ~FileAttributes.ReadOnly;
                }

                // Remove readonly from all files
                foreach (var file in directory.GetFiles())
                {
                    try
                    {
                        if (file.Attributes.HasFlag(FileAttributes.ReadOnly))
                        {
                            file.Attributes &= ~FileAttributes.ReadOnly;
                        }
                    }
                    catch
                    {
                        // Continue with other files
                    }
                }

                // Recursively process subdirectories
                foreach (var subdir in directory.GetDirectories())
                {
                    try
                    {
                        RemoveReadOnlyAttribute(subdir);
                    }
                    catch
                    {
                        // Continue with other directories
                    }
                }
            }
            catch
            {
                // Ignore errors and continue
            }
        }

        private static string GetRepoNameFromUrl(string url)
        {
            var cleaned = url.Trim().TrimEnd('/');

            if (cleaned.Contains(':') && cleaned.StartsWith("git@"))
            {
                // git@github.com:owner/repo(.git)
                cleaned = cleaned.Split(':').Last();
            }

            var last = cleaned.Split('/').Last();
            if (last.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
            {
                last = last.Substring(0, last.Length - 4);
            }
            return last;
        }
    }
}

