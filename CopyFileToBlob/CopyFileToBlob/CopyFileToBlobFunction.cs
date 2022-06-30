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
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "CopyBlob")] HttpRequest req,
        CopyParameters parameters,
        ILogger log)
    {
        log.LogInformation($"Blob copy started.");

        BlobClient destinationBlob = new(new Uri(parameters.DestinationBlobUrl));

        CopyFromUriOperation operation = await destinationBlob.StartCopyFromUriAsync(new Uri(parameters.SourceBlobUrl));

        _ = await operation.WaitForCompletionAsync();

        string message = $"Blob Copy successful";
        log.LogInformation(message);
        return new OkObjectResult(message);
    }
}