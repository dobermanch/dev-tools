using System.ComponentModel;

namespace Dev.Tools.Core.Localization;

public class LocalizedDescriptionAttribute(string description) : DescriptionAttribute(description)
{
    public override string Description => base.Description;
}