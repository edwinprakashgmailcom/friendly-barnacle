using Microsoft.Graph;
using Microsoft.Identity.Client;

namespace ConsoleApp;

public class Uploader
{
    private readonly AzureAdConfiguration _config;
    private readonly GraphServiceClient _graphServiceClient;

    public Uploader(AzureAdConfiguration config)
    {
        _config = config;
        _graphServiceClient = GetAuthenticatedGraphClient();
    }

    /// <summary>
    /// Use this to upload files less than 4MB in size
    /// </summary>
    /// <returns></returns>
    public async Task<DriveItem> UploadSmallFileAsync()
    {
        const string file = @"SampleFiles\SmallFile.txt";

        using var fileStream = new FileStream(file, FileMode.Open);

        var driveItem = await _graphServiceClient
            .Sites[_config.SiteId]
            .Lists[_config.ListId]
            .Drive.Root.ItemWithPath($"Circulation/TOPNZ%20load/Pivotal%20data%20extracts/{file}")
            .Content.Request().PutAsync<DriveItem>(fileStream);

        return driveItem;
    }

    /// <summary>
    /// Use this to upload files more than 4MB in size
    /// </summary>
    /// <returns></returns>
    public async Task<DriveItem?> UploadLargeFileAsync()
    {
        DriveItem? driveItem = null;
        const string file = @"SampleFiles\LargeFile.txt";

        using var fileStream = new FileStream(file, FileMode.Open);

        var uploadSession = await _graphServiceClient
            .Sites[_config.SiteId]
            .Lists[_config.ListId]
            .Drive.Root.ItemWithPath($"Circulation/TOPNZ%20load/Pivotal%20data%20extracts/{file}")
            .CreateUploadSession().Request().PostAsync();
        
        // Chunk size must be divisible by 320KiB, our chunk size will be slightly more than 1MB
        int maxSizeChunk = (320 * 1024) * 4;
        var fileUploadTask = new LargeFileUploadTask<DriveItem>(uploadSession, fileStream, maxSizeChunk);

        // Create a callback that is invoked after each slice is uploaded
        IProgress<long> progress = new Progress<long>(prog =>
        {
            Console.WriteLine($"Uploaded {prog} bytes of {fileStream.Length} bytes");
        });


        try
        {
            // Upload the file
            var uploadResult = await fileUploadTask.UploadAsync(progress);

            if (uploadResult.UploadSucceeded)
            {
                driveItem = uploadResult.ItemResponse;
                Console.WriteLine("Upload completed");
            }
            else
            {
                Console.WriteLine("Upload failed");
            }
        }
        catch (ServiceException ex)
        {
            Console.WriteLine($"Error uploading: {ex}");
        }
        return driveItem;
    }

    private GraphServiceClient GetAuthenticatedGraphClient()
    {
        var authority = $"https://login.microsoftonline.com/{_config.TenantId}/v2.0";

        List<string> scopes = new List<string>();
        scopes.Add("https://graph.microsoft.com/.default");

        var cca = ConfidentialClientApplicationBuilder.Create(_config.ApplicationId)
                                                .WithAuthority(authority)
                                                .WithClientSecret(_config.ApplicationSecret)
                                                .Build();
        
        return new GraphServiceClient(new MsalAuthenticationProvider(cca, scopes.ToArray()));
    }

}
