using System.Linq.Expressions;

namespace Dev.Tools.Web.Extensions;

public static class ExpressionExtensions
{
    public static TKey GetValue<TData, TKey>(this Expression<Func<TData, TKey>> expression, TData data)
    {
        if (expression == null) throw new ArgumentNullException(nameof(expression));
        if (data == null) throw new ArgumentNullException(nameof(data));

        var compiledExpression = expression.Compile();
        return compiledExpression(data);
    }
}