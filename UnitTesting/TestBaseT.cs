using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTesting
{
    public abstract class TestBase<TSubject> : TestBase where TSubject : new()
    {
        public TSubject Subject { get; set; }

        [SetUp]
        public void ConstructSubject()
        {
            Subject = new TSubject();
        }
    }
}
