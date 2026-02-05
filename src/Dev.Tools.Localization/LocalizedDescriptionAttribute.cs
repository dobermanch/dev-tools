using System.ComponentModel;
using Microsoft.Extensions.Localization;

namespace Dev.Tools.Localization;

public class LocalizedDescriptionAttribute(string description) : DescriptionAttribute(description)
{
    public override string Description => LocalizationProvider.Current.GetString(DescriptionValue);
}