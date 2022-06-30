namespace CopyBlob;

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using System;
using System.IO;
using System.Threading.Tasks;

public static class CopyBlobFunction
{
    [FunctionName("CopyBlob")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
        ILogger log)
    {
        CopyParameters parameters = new();
        BlobProperties destinationBlobInfo = null;
        BlobProperties sourceBlobInfo = null;
        int retry = 0;
        try
        {
            string requestBody = String.Empty;
            using (StreamReader streamReader = new(req.Body))
            {
                requestBody = await streamReader.ReadToEndAsync();
            }
            parameters = JsonConvert.DeserializeObject<CopyParameters>(requestBody);

            if (string.IsNullOrWhiteSpace(parameters.SourceBlobUrl))
            {
                throw new ArgumentException(nameof(parameters.SourceBlobUrl));
            }
            if (string.IsNullOrWhiteSpace(parameters.DestinationBlobUrl))
            {
                throw new ArgumentException(nameof(parameters.DestinationBlobUrl));
            }
            log.LogInformation($"Blob copy started.\n{parameters}");

            BlobClient sourceBlob = new(new Uri(parameters.SourceBlobUrl));
            BlobClient destinationBlob = new(new Uri(parameters.DestinationBlobUrl));

            if (!await sourceBlob.ExistsAsync())
            {
                throw new Exception($"Source Blob {new Uri(parameters.SourceBlobUrl).AbsoluteUri} does not exist.");
            }

            BlobLeaseClient lease = sourceBlob.GetBlobLeaseClient();
            _ = await lease.AcquireAsync(TimeSpan.FromSeconds(-1));
            try
            {
                sourceBlobInfo = await sourceBlob.GetPropertiesAsync();
                log.LogInformation($"Source Blob information : {sourceBlobInfo}");

                CopyFromUriOperation operation = await destinationBlob.StartCopyFromUriAsync(sourceBlob.Uri);

                do
                {
                    destinationBlobInfo = (await destinationBlob.GetPropertiesAsync()).Value;
                    log.LogInformation($"Copy Blob information : {destinationBlobInfo}");

                    if (destinationBlobInfo.BlobCopyStatus == CopyStatus.Pending)
                    {
                        if (++retry > 200)
                        {
                            throw new Exception($"The copy is still pending after {retry} seconds.");
                        }
                        await Task.Delay(1000);
                    }
                    else
                    {
                        break;
                    }
                }
                while (true);
            }
            finally
            {
                _ = await lease.BreakAsync();
            }
        }
        catch (Exception e)
        {
            log.LogError(e, $"Blob Copy failed.\n{parameters}\n{sourceBlobInfo}\n{destinationBlobInfo}");
            return new BadRequestObjectResult(e);
        }
        string message = $"Blob Copy successful.\n{parameters}\n{sourceBlobInfo}\n{destinationBlobInfo}";
        log.LogInformation(message);
        return new OkObjectResult(message);
    }
}