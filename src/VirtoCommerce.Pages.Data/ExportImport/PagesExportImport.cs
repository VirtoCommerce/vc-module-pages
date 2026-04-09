using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using VirtoCommerce.Pages.Core.Models;
using VirtoCommerce.Pages.Core.Search;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.ExportImport;

namespace VirtoCommerce.Pages.Data.ExportImport;

public class PagesExportImport(IPageDocumentSearchService searchService)
{
    private const int BatchSize = 50;

    public async Task DoExportAsync(Stream outStream, Action<ExportImportProgressInfo> progressCallback, ICancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var progressInfo = new ExportImportProgressInfo { Description = "Loading pages..." };
        progressCallback(progressInfo);

        await using var streamWriter = new StreamWriter(outStream, leaveOpen: true);
        await using var jsonWriter = new JsonTextWriter(streamWriter);

        var serializer = new JsonSerializer();

        await jsonWriter.WriteStartObjectAsync();
        await jsonWriter.WritePropertyNameAsync("Pages");
        await jsonWriter.WriteStartArrayAsync();

        var criteria = AbstractTypeFactory<PageDocumentSearchCriteria>.TryCreateInstance();
        criteria.Take = BatchSize;
        criteria.Skip = 0;

        int totalCount;
        do
        {
            cancellationToken.ThrowIfCancellationRequested();

            var searchResult = await searchService.SearchAsync(criteria, clone: false);
            totalCount = searchResult.TotalCount;

            if (searchResult.Results.Count == 0)
            {
                break;
            }

            foreach (var page in searchResult.Results)
            {
                serializer.Serialize(jsonWriter, page);
            }

            criteria.Skip += BatchSize;

            progressInfo.Description = $"Exporting pages: {Math.Min(criteria.Skip, totalCount)} of {totalCount}";
            progressInfo.ProcessedCount = Math.Min(criteria.Skip, totalCount);
            progressInfo.TotalCount = totalCount;
            progressCallback(progressInfo);
        }
        while (criteria.Skip < totalCount);

        await jsonWriter.WriteEndArrayAsync();
        await jsonWriter.WriteEndObjectAsync();

        await jsonWriter.FlushAsync();
    }

    public async Task DoImportAsync(Stream inputStream, Action<ExportImportProgressInfo> progressCallback, ICancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var progressInfo = new ExportImportProgressInfo { Description = "Importing pages..." };
        progressCallback(progressInfo);

        using var streamReader = new StreamReader(inputStream);
        using var jsonReader = new JsonTextReader(streamReader);

        var serializer = new JsonSerializer();

        while (await jsonReader.ReadAsync())
        {
            if (jsonReader.TokenType != JsonToken.PropertyName)
            {
                continue;
            }

            switch (jsonReader.Value?.ToString())
            {
                case "Pages":
                    await ImportPagesAsync(jsonReader, serializer, progressCallback, cancellationToken);
                    break;
            }
        }
    }

    private async Task ImportPagesAsync(
        JsonTextReader jsonReader,
        JsonSerializer serializer,
        Action<ExportImportProgressInfo> progressCallback,
        ICancellationToken cancellationToken)
    {
        var progressInfo = new ExportImportProgressInfo { Description = "Importing pages..." };
        var processedCount = 0;

        await jsonReader.ReadAsync(); // StartArray

        var batch = new PageDocument[BatchSize];
        var batchIndex = 0;

        while (await jsonReader.ReadAsync())
        {
            if (jsonReader.TokenType == JsonToken.EndArray)
            {
                break;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var page = serializer.Deserialize<PageDocument>(jsonReader);

            if (page != null)
            {
                batch[batchIndex++] = page;

                if (batchIndex >= BatchSize)
                {
                    await searchService.IndexDocuments(batch.Take(batchIndex).ToArray());
                    processedCount += batchIndex;
                    batchIndex = 0;

                    progressInfo.Description = $"Importing pages: {processedCount} processed";
                    progressInfo.ProcessedCount = processedCount;
                    progressCallback(progressInfo);
                }
            }
        }

        // Flush remaining batch
        if (batchIndex > 0)
        {
            await searchService.IndexDocuments(batch.Take(batchIndex).ToArray());
            processedCount += batchIndex;
        }

        progressInfo.Description = $"Pages import completed: {processedCount} pages imported";
        progressInfo.ProcessedCount = processedCount;
        progressCallback(progressInfo);
    }
}
