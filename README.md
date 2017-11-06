# DownloadFileWithProgress
Downloading a file from an ASP.net Core Web API project 

Whilst working on a Xamarin forms application I wasn't able to find a straight forward example of downloading a file via the HttpClient which also has progress reporting and the option to cancel a download.

A simple ASP.net Core Web API method returns a collection of numbers

```
// GET api/values
[HttpGet]
public IEnumerable<string> Get()
{
    IEnumerable<string> randomText = Enumerable.Range(0, 999999).Select(s => s.ToString()).ToArray();
    string serialised = JsonConvert.SerializeObject(randomText);
    byte[] bytes = Encoding.UTF8.GetBytes(serialised);
    Response.ContentLength = bytes.Length;

    return randomText;
}
```

And the client calls ReadAsStreamAsync and polls for the result and raises and event with the progress.

```
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
```
