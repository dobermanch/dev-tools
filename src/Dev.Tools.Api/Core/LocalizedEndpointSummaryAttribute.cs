using Dev.Tools.Localization;
using Microsoft.AspNetCore.Http.Metadata;

namespace Dev.Tools.Api.Core;

public class LocalizedEndpointSummaryAttribute(string summary) 
    : LocalizedDescriptionAttribute(summary), IEndpointSummaryMetadata
{
    public string Summary => Description;
}