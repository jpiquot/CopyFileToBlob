namespace CopyFileToBlob;

using Azure.Storage.Blobs;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

using System;
using System.IO;
using System.Threading.Tasks;

public static class CopyFileToBlobFunction
{
    [FunctionName("CopyFileToBlob")]
    [ActionName("Copy")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "CopyFileToBlob/{fileShare}/{fileName}")] HttpRequest req,
        [File("{fileShare}/{fileName}", FileAccess.Read)] Stream sourceStream,
        string fileShare,
        string fileName,
        Uri destinatioBlobUrl,
        ILogger log)
    {
        log.LogInformation($"Copy file '{fileShare}/{fileName}' to Blob started.");

        BlobClient blobClient = new(destinatioBlobUrl);
        _ = await blobClient.UploadAsync(sourceStream, overwrite: true);

        string message = $"File '{fileShare}/{fileName}' copied to Blob {blobClient.AccountName}/{blobClient.BlobContainerName}/{blobClient.Name} ended.";
        log.LogInformation(message);
        return new OkObjectResult(message);
    }
}