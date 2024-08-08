using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using VirtoCommerce.Pages.Core.BuilderIO.Converters;

namespace VirtoCommerce.Pages.Core.BuilderIO.Models;

public class BuilderIOPage
{
    [JsonProperty("@version")]
    public int Version { get; set; }
    public string CreatedBy { get; set; }

    [JsonConverter(typeof(UnixMillisecondsConverter))]
    public DateTime CreatedDate { get; set; }
    [JsonConverter(typeof(UnixMillisecondsConverter))]
    public DateTime StartDate { get; set; }
    [JsonConverter(typeof(UnixMillisecondsConverter))]
    public DateTime EndDate { get; set; }
    public Dictionary<string, object> Data { get; set; }

    [JsonConverter(typeof(UnixMillisecondsConverter))]
    public DateTime? FirstPublished { get; set; }
    public string Id { get; set; }
    public string LastUpdateBy { get; set; }

    [JsonConverter(typeof(UnixMillisecondsConverter))]
    public DateTime? LastUpdated { get; set; }
    public string LastUpdatedBy { get; set; }
    public PageMetadata Meta { get; set; }
    public string ModelId { get; set; }
    public string Name { get; set; }
    public string OwnerId { get; set; }
    public int Priority { get; set; }
    public string Published { get; set; }
    public PageQuery[] Query { get; set; }
}

