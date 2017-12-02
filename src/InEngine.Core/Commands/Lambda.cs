using System;
using System.Linq.Expressions;
using Serialize.Linq.Extensions;
using Serialize.Linq.Nodes;

namespace InEngine.Core.Commands
{
    public class Lambda : AbstractCommand
    {
        public ExpressionNode ExpressionNode { get; set; }
        public Expression<Action> ExpressionAction { 
            get { return ExpressionNode.ToExpression<Action>(); }
            set { ExpressionNode = value.ToExpressionNode(); } 
        }

        public Lambda()
        {}

        public Lambda(Expression<Action> expressionAction) : this()
        {
            ExpressionAction = expressionAction;
        }

        public override void Run()
        {
            ExpressionNode.ToExpression<Action>().Compile()();
        }
    }
}
