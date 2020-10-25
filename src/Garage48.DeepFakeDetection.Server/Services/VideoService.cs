using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Garage48.DeepFakeDetection.Server.Services
{
    public sealed class VideoService : IDisposable
    {
        private readonly ConcurrentDictionary<Guid, ConcurrentQueue<ArraySegment<byte>>> _segmentBuffer = new ConcurrentDictionary<Guid, ConcurrentQueue<ArraySegment<byte>>>();

        private readonly ConcurrentDictionary<Guid, Task> _writerTasks = new ConcurrentDictionary<Guid, Task>();

        private readonly ConcurrentDictionary<Guid, ConcurrentQueue<string>> _responses = new ConcurrentDictionary<Guid, ConcurrentQueue<string>>();

        private readonly CancellationTokenSource _cts;

        private readonly ILogger<VideoService> _logger;

        private readonly HttpClient _client;

        public VideoService(ILogger<VideoService> logger)
        {
            _logger = logger;
            _client = new HttpClient
            {
                // BaseAddress = new Uri("https://ml-core-api-catsiba3zq-lz.a.run.app"),
                BaseAddress = new Uri("http://localhost:8000"),
                Timeout = TimeSpan.FromMinutes(5)
            };
            _cts = new CancellationTokenSource();
        }

        public async Task<(ArraySegment<byte>, string? latestResult)> HandleSegmentAsync(Guid clientId, ArraySegment<byte> videoSegment)
        {
            if (!_writerTasks.ContainsKey(clientId))
            {
                _writerTasks.TryAdd(clientId, FileWriter(clientId, _cts.Token));
            }

            _segmentBuffer.GetOrAdd(clientId, _ => new ConcurrentQueue<ArraySegment<byte>>()).Enqueue(videoSegment);
            if (_responses.TryGetValue(clientId, out var queue) && queue.TryDequeue(out var evalResult))
            {
                return (videoSegment, evalResult);
            }

            return (videoSegment, null);
        }

        private async Task FileWriter(Guid clientId, CancellationToken cancellationToken)
        {
            var fileName = $"{clientId:N}.webm";
            var queue = _segmentBuffer.GetOrAdd(clientId, _ => new ConcurrentQueue<ArraySegment<byte>>());
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var writeCounter = 0;
                    await using (var fileStream = File.Open(fileName, FileMode.Append))
                    {
                        while (writeCounter < 10 && !cancellationToken.IsCancellationRequested)
                        {
                            if (!queue.TryDequeue(out var arraySegment))
                            {
                                await Task.Delay(10, cancellationToken);
                                continue;
                            }

                            await fileStream.WriteAsync(arraySegment, cancellationToken);
                            ++writeCounter;
                        }
                    }

                    // redo metadata, so that ffmpeg knows when video ends
                    RedoMetadata(fileName);
                    CopyLast10SecondsToMp4(clientId);
                    var mp4FileName = $"{clientId:N}.mp4";
                    var bytes = await File.ReadAllBytesAsync(mp4FileName, cancellationToken);

                    var content = new MultipartFormDataContent
                    {
                        new ByteArrayContent(bytes)
                        {
                            Headers =
                            {
                                ContentType = MediaTypeHeaderValue.Parse("video/mp4"),
                                ContentDisposition = new ContentDispositionHeaderValue("form-data")
                                {
                                    FileName = mp4FileName,
                                    Name = "file"
                                }
                            }
                        },
                    };

                    _logger.LogInformation("Sending request to the model server..");
                    var response = await _client.PostAsync("/file", content, cancellationToken);
                    var evalResult = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation(evalResult);
                    _responses.GetOrAdd(clientId, _ => new ConcurrentQueue<string>()).Enqueue(evalResult);
                }
                catch (Exception e)
                {
                    _logger.LogError(e.Message);
                }
            }
        }

        private void RedoMetadata(string fileName)
        {
            using var process = new Process();
            process.StartInfo.FileName = "ffmpeg";
            process.StartInfo.Arguments =
                $"-i {fileName} -acodec copy -vcodec copy -map_metadata -1 copy-{fileName} -y";

            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();

            if (!process.WaitForExit(1000 * 60))
            {
                throw new TimeoutException("ffmpeg took too long to run");
            }
        }

        private void CopyLast10SecondsToMp4(Guid clientId)
        {
            using var process = new Process();
            process.StartInfo.FileName = "ffmpeg";
            process.StartInfo.Arguments =
                $"-sseof -10 -i copy-{clientId:N}.webm {clientId:N}.mp4 -y";

            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();

            if (!process.WaitForExit(1000 * 60))
            {
                throw new TimeoutException("ffmpeg took too long to run");
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            foreach (var task in _writerTasks.Values)
            {
                task.Dispose();
            }
        }
    }
}