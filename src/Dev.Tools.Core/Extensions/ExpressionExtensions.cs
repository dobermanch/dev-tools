using System.Linq.Expressions;

namespace Dev.Tools.Core.Extensions;

public static class ExpressionExtensions
{
    public static TKey GetValue<TData, TKey>(this Expression<Func<TData, TKey>> expression, TData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(expression);

        var compiledExpression = expression.Compile();
        return compiledExpression(data);
    }
}