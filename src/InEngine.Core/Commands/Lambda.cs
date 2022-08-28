using System;
using System.Threading.Tasks;
using Serialize.Linq.Nodes;

namespace InEngine.Core.Commands;

public class Lambda : AbstractCommand
{
    public ExpressionNode ExpressionNode { get; set; }

    public Lambda()
    {
    }

    public Lambda(ExpressionNode expressionNode) : this() => ExpressionNode = expressionNode;

    public override async Task Run()
    {
        var function = ExpressionNode.ToExpression<Action>().Compile();
        await Task.Run(() => function());
    }
}