using System.Collections;
using System.Linq.Expressions;
using Dev.Tools.Core;
using Dev.Tools.Core.Extensions;
using Dev.Tools.Providers;
using Microsoft.AspNetCore.Components;

namespace Dev.Tools.Web.Components.Tools;

public partial class ToolsList : ComponentBase
{
    [Inject] private IToolsProvider ToolsProvider { get; set; } = default!;

    private GroupedList<string, ToolDefinition> Tools { get; set; } = GroupedList<string, ToolDefinition>.Empty;

    protected override void OnInitialized()
    {
        var tools = ToolsProvider.GetTools();
            
        Tools = new GroupedList<string, ToolDefinition>(it => it.Name, tools);
        
        base.OnInitialized();
    }

    protected override void OnParametersSet()
    {
        
        base.OnParametersSet();
    }
}

public record GroupedList<TKey, TData> : IEnumerable<GroupedItem<TKey, TData>>
{
    public static readonly GroupedList<TKey, TData> Empty = new([]);
    
    private readonly IList<GroupedItem<TKey, TData>> _grouped;

    public GroupedList(IList<GroupedItem<TKey, TData>> items) => _grouped = items;

    public GroupedList(Expression<Func<TData, TKey>> keySelector, IEnumerable<TData> data)
    {
        _grouped = data
            .GroupBy(keySelector.GetValue, it => it)
            .Select(it => new GroupedItem<TKey, TData>
            {
                Key = it.Key,
                Data = it.ToArray()
            })
            .ToArray();
    }

    public IEnumerator<GroupedItem<TKey, TData>> GetEnumerator()
        => _grouped.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}

public record GroupedItem<TKey, TData> : IEnumerable<TData>
{
    public TKey Key { get; init; } = default!;

    public IList<TData> Data { get; init; } = [];
    
    public IEnumerator<TData> GetEnumerator() => Data.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}