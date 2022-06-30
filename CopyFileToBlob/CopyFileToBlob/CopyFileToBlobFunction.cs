namespace CopyBlob;

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

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
        try
        {
            string requestBody = String.Empty;
            using (StreamReader streamReader = new StreamReader(req.Body))
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

            BlobClient destinationBlob = new(new Uri(parameters.DestinationBlobUrl));

            CopyFromUriOperation operation = await destinationBlob.StartCopyFromUriAsync(new Uri(parameters.SourceBlobUrl));

            _ = await operation.WaitForCompletionAsync();
        }
        catch (Exception e)
        {
            log.LogError(e, $"Blob Copy failed.\n{parameters}");
            return new BadRequestObjectResult(e);
        }
        string message = $"Blob Copy successful.\n{parameters}";
        await Task.Delay(1000);
        log.LogInformation(message);
        return new OkObjectResult(message);
    }
}