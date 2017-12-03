using System;
using System.Linq.Expressions;
using Serialize.Linq.Extensions;
using Serialize.Linq.Nodes;

namespace InEngine.Core.Commands
{
    public class Lambda : AbstractCommand
    {
        public ExpressionNode ExpressionNode { get; set; }

        public Lambda()
        {}

        public Lambda(ExpressionNode expressionNode) : this()
        {
            ExpressionNode = expressionNode;
        }

        public override void Run()
        {
            ExpressionNode.ToExpression<Action>().Compile()();
        }
    }
}
