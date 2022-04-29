using ConsoleApp;

var configReader = new ConfigurationReader();
var azureAdConfig = configReader.ReadSection<AzureAdConfiguration>("AzureAd");

var uploader = new Uploader(azureAdConfig);

var result = await uploader.UploadLargeFileAsync();

Console.ReadKey();