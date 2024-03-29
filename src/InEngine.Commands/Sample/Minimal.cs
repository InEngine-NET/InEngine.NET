﻿using System.Threading.Tasks;
using InEngine.Core;

namespace InEngine.Commands.Sample;

using System;

/*
 * At a minimum, a command must implement the AbstractCommand interface.
 *
 * A command result must be returned from the Run method.
 * The command result has an optional message. This is especially helpful
 * when the command does not finish successfully.
 */
public class Minimal : AbstractCommand
{
    public override void Run() => Line("This is an example of a minimal command.");
}
