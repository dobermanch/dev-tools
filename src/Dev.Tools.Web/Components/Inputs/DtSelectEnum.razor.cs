using Microsoft.AspNetCore.Components;

namespace Dev.Tools.Web.Components.Inputs;

public partial class DtSelectEnum<T> : ComponentBase
    where T: struct, Enum
{
    [Parameter] 
    public string Label { get; set; } = null!;
    
    [Parameter]
    public T Value { get; set; }
    
    [Parameter]
    public EventCallback<T> ValueChanged { get; set; }
    
    [Parameter]
    public LabelPositionType LabelPosition { get; set; }
    
    private T[] GetEnumValues()
    {
        return Enum.GetValues<T>();
    }

    [Parameter] 
    public Func<T, string>? ValueToDisplay { get; set; }
}