using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace DownloadFileWithProgress
{
    class MainClass
    {
        private const string APIURL = "http://localhost:5000";
        public static EventHandler<int> DownloadProgressUpdated;
        private static int previousProgress;

        public static void Main(string[] args)
        {
            DownloadProgressUpdated += (sender, e) => Console.WriteLine(e.ToString());
            var cancellationToken = new CancellationTokenSource();

            DownloadFileAsync("api/values/", cancellationToken.Token).Wait();
        }

        public static async Task<byte[]> DownloadFileAsync(string url, CancellationToken token)
        {
            previousProgress = 0;
            var client = new HttpClient();
            client.BaseAddress = new Uri(APIURL);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(string.Format("The request returned with HTTP status code {0}", response.StatusCode));
            }

            var total = response.Content.Headers.ContentLength.HasValue ? response.Content.Headers.ContentLength.Value : -1L;
            var canReportProgress = total != -1 && DownloadProgressUpdated != null;

            using (var stream = await response.Content.ReadAsStreamAsync())
            {
                var totalRead = 0;
                var buffer = new byte[total == -1 ? 4096 : total];
                var isMoreToRead = true;

                do
                {
                    token.ThrowIfCancellationRequested();

                    var read = await stream.ReadAsync(buffer, totalRead, buffer.Length - totalRead, token);

                    if (read == 0)
                    {
                        isMoreToRead = false;
                    }
                    else
                    {
                        totalRead += read;

                        if (canReportProgress)
                        {
                            double progress = ((totalRead * 1d) / (total * 1d) * 100);
                            UpdateDownloadProgress(progress);
                        }
                    }
                } while (isMoreToRead);

                UpdateDownloadProgress(100);

                return buffer;
            }
        }

        private static void UpdateDownloadProgress(double progress)
        {
            int currentProgress = (int)Math.Round(progress, 0);

            if (currentProgress == previousProgress)
                return;
            
            DownloadProgressUpdated?.Invoke(null, currentProgress);
            previousProgress = currentProgress;
        }
    }
}
