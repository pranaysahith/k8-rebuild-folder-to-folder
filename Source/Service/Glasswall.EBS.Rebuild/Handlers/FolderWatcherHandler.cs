using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;
using Glasswall.EBS.Rebuild.Response;
using System.Collections.Generic;

namespace Glasswall.EBS.Rebuild.Handlers
{
    public class FolderWatcherHandler : IFolderWatcherHandler
    {
        private readonly ILogger<FolderWatcherHandler> _logger;
        private readonly IHttpHandler _httpHandler;
        private Timer _timer;

        public FolderWatcherHandler(string path, ILogger<FolderWatcherHandler> logger, IHttpHandler httpHandler)
        {
            Path = path;
            _logger = logger;
            _httpHandler = httpHandler;
        }

        public string Path { get; private set; }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            double.TryParse(Environment.GetEnvironmentVariable(Constants.EnvironmentVariables.CronjobPeriod), out double cronjobPeriodInSeconds);
            _timer = new Timer(PullFolder, null, TimeSpan.Zero, TimeSpan.FromSeconds(cronjobPeriodInSeconds));
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private async void PullFolder(object state)
        {
            await ProcessFolder();
        }

        public async Task ProcessFolder()
        {
            string processingFolderPath = System.IO.Path.Combine(Environment.GetEnvironmentVariable(Constants.EnvironmentVariables.ForldersPath), Constants.ProcessingFolder);
            string tempFolderPath = System.IO.Path.Combine(processingFolderPath, Guid.NewGuid().ToString());
            try
            {
                MoveInputFilesToTempLocation(tempFolderPath);
                foreach (string file in Directory.EnumerateFiles(tempFolderPath, Constants.ZipSearchPattern))
                {
                    MultipartFormDataContent multiFormData = new MultipartFormDataContent();
                    FileStream fs = File.OpenRead(file);
                    multiFormData.Add(new StreamContent(fs), Constants.FileKey, System.IO.Path.GetFileName(file));
                    string url = $"{Environment.GetEnvironmentVariable(Constants.EnvironmentVariables.RebuildApiBaseUrl)}{Constants.ZipFileApiPath}";
                    IApiResponse response = await _httpHandler.PostAsync(url, multiFormData);
                    string rawFilePath = file.Substring(0, file.Substring(0, file.LastIndexOf("/")).LastIndexOf("/"));
                    rawFilePath = rawFilePath.Substring(0, rawFilePath.LastIndexOf("/"));
                    string destinationPath = string.Empty;
                    if (response.Success)
                    {
                        destinationPath = System.IO.Path.Combine(rawFilePath, Constants.OutputFolder, System.IO.Path.GetFileName(file));
                        destinationPath = NextAvailableFilename(destinationPath);
                        using (FileStream fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write))
                        {
                            if (response.Content != null)
                                await response.Content.CopyToAsync(fileStream);
                        }
                        _logger.LogInformation($"Successfully processed the file {System.IO.Path.GetFileName(file)}");
                    }
                    else
                    {
                        destinationPath = System.IO.Path.Combine(rawFilePath, Constants.ErrorFolder, System.IO.Path.GetFileName(file));
                        destinationPath = NextAvailableFilename(destinationPath);
                        File.Move(file, destinationPath);
                        _logger.LogInformation($"Error while processing the file {System.IO.Path.GetFileName(file)} and error is {response.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occured while processing folder errorMessage: {ex.Message} and errorStackTrace: {ex.StackTrace}");
            }
            finally
            {
                if (Directory.Exists(tempFolderPath))
                {
                    Directory.Delete(tempFolderPath, true);
                }
            }
        }

        private void MoveInputFilesToTempLocation(string tempFolderPath)
        {
            if (!Directory.Exists(tempFolderPath))
            {
                Directory.CreateDirectory(tempFolderPath);
            }

            foreach (string file in Directory.EnumerateFiles(Path, Constants.ZipSearchPattern))
            {
                string destFile = System.IO.Path.Combine(tempFolderPath, System.IO.Path.GetFileName(file));
                if (!File.Exists(destFile))
                    File.Move(file, destFile);
            }
        }

        private string NextAvailableFilename(string path)
        {
            string numberPattern = " ({0})";

            if (!File.Exists(path))
                return path;

            if (System.IO.Path.HasExtension(path))
                return GetNextFilename(path.Insert(path.LastIndexOf(System.IO.Path.GetExtension(path)), numberPattern));

            return GetNextFilename(path + numberPattern);
        }

        private string GetNextFilename(string pattern)
        {
            string tmp = string.Format(pattern, 1);
            if (tmp == pattern)
                throw new ArgumentException("The pattern must include an index place-holder", "pattern");

            if (!File.Exists(tmp))
                return tmp;

            int min = 1, max = 2;

            while (File.Exists(string.Format(pattern, max)))
            {
                min = max;
                max *= 2;
            }

            while (max != min + 1)
            {
                int pivot = (max + min) / 2;
                if (File.Exists(string.Format(pattern, pivot)))
                    min = pivot;
                else
                    max = pivot;
            }

            return string.Format(pattern, max);
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}