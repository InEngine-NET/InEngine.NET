using InEngine.Core;
using System;

namespace InEngine.Commands.Sample;

public class SayHello : AbstractCommand
{
    public override void Run()
    {
        Console.WriteLine("hello");
    }
}