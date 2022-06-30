namespace CopyBlob;

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

using System;
using System.Threading.Tasks;

public static class CopyBlobFunction
{
    [FunctionName("CopyBlob")]
    [ActionName("Copy")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
        CopyParameters parameters,
        ILogger log)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(parameters.SourceBlobUrl))
            {
                throw new ArgumentNullException(nameof(parameters.SourceBlobUrl));
            }
            if (string.IsNullOrWhiteSpace(parameters.DestinationBlobUrl))
            {
                throw new ArgumentNullException(nameof(parameters.DestinationBlobUrl));
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
        log.LogInformation(message);
        return new OkObjectResult(message);
    }
}