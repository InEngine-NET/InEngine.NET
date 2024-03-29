﻿using System;
using Serialize.Linq.Nodes;

namespace InEngine.Core.Commands;

public class Lambda : AbstractCommand
{
    public ExpressionNode ExpressionNode { get; set; }

    public Lambda()
    {
    }

    public Lambda(ExpressionNode expressionNode) : this() => ExpressionNode = expressionNode;

    public override void Run()
    {
        var function = ExpressionNode.ToExpression<Action>().Compile();
        function();
    }
}