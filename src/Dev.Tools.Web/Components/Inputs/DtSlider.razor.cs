using System.Numerics;
using Microsoft.AspNetCore.Components;

namespace Dev.Tools.Web.Components.Inputs;

public partial class DtSlider<T> : ComponentBase
    where T : struct, INumber<T>
{
    [Parameter]
    public string? Class { get; set; }

    [Parameter] 
    public string Label { get; set; } = null!;
    
    [Parameter]
    public T MinValue { get; set; }
    
    [Parameter]
    public T MaxValue { get; set; }
    
    [Parameter]
    public T Value { get; set; }
    
    [Parameter]
    public EventCallback<T> ValueChanged { get; set; }   
}