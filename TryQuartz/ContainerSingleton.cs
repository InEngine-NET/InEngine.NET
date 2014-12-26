﻿using System;
using Funq;

namespace TryQuartz
{
    public class ContainerSingleton
    {
        private static Container _container;
        private ContainerSingleton() {}

        public static Container GetContainer()
        {
            if (_container == null)
                _container = new Container();
            return _container;
        }
    }
}
