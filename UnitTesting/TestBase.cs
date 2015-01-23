using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTesting
{
    public abstract class TestBase
    {
        /// <summary>
        /// Creates & returns a subdirectory under TestContext.CurrentContext.TestDirectory named for the test class
        /// </summary>
        protected virtual string TestDirectory()
        {
            var stackFrames = new StackTrace(1).GetFrames();
            var testMethodFrame = stackFrames.SingleOrDefault(f => f.GetMethod().Name == TestContext.CurrentContext.Test.Name)
                ?? stackFrames.First(f => f.GetMethod().DeclaringType.Name.EndsWith("Test"));
            var testClassName = testMethodFrame.GetMethod().DeclaringType.Name;
            Assert.That(testClassName.EndsWith("Test"), "Sanity check: We expect the test class to end with 'Test'");
            var directory = Path.Combine(TestContext.CurrentContext.TestDirectory, testClassName);
            Directory.CreateDirectory(directory);
            return directory;
        }

        /// <summary>
        /// Returns a path like {Test execution directory}/{Test Class}/{Test Method}.
        /// It creates the directory if necessary.
        /// </summary>
        protected virtual string TestFilePath()
        {
            return Path.Combine(TestDirectory(), TestContext.CurrentContext.Test.Name);
        }
    }
}
