using Dev.Tools.Localization;
using Microsoft.AspNetCore.Http.Metadata;

namespace Dev.Tools.Api.Core;

public class LocalizedEndpointDescriptionAttribute(string description) 
    : LocalizedDescriptionAttribute(description), IEndpointDescriptionMetadata;