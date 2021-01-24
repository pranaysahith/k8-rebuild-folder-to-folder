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
            string url = $"{Environment.GetEnvironmentVariable(Constants.EnvironmentVariables.RebuildApiBaseUrl)}{Constants.ZipFileApiPath}";
            try
            {
                foreach (string file in Directory.EnumerateFiles(Path, Constants.ZipSearchPattern))
                {
                    MultipartFormDataContent multiFormData = new MultipartFormDataContent();
                    FileStream fs = File.OpenRead(file);
                    multiFormData.Add(new StreamContent(fs), Constants.FileKey, System.IO.Path.GetFileName(file));
                    IApiResponse response = await _httpHandler.PostAsync(url, multiFormData);
                    string rawFilePath = file.Substring(0, file.Substring(0, file.LastIndexOf("/")).LastIndexOf("/"));
                    string destinationPath = string.Empty;
                    if (response.Success)
                    {
                        destinationPath = System.IO.Path.Combine(rawFilePath, Constants.OutputFolder, System.IO.Path.GetFileName(file));
                        using (FileStream fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write))
                        {
                            await response.Content.CopyToAsync(fileStream);
                        }
                        File.Delete(file);
                        _logger.LogInformation($"Successfully processed the file {file}");
                    }
                    else
                    {
                        destinationPath = System.IO.Path.Combine(rawFilePath, Constants.ErrorFolder, System.IO.Path.GetFileName(file));
                        File.Move(file, destinationPath, true);
                        _logger.LogInformation($"Error while processing the file {file} and error is {response.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occured while processing folder", ex.Message);
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}