using System;
using RDotNet;
using System.Linq;

namespace TryQuartz
{
    public class RAnalysisJob
    {
        public REngine REngine { get; set; }

        public RAnalysisJob()
        {
            REngine = ContainerSingleton.GetContainer().Resolve<REngine>();
        }

        public void Execute()
        {
//            REngine.Evaluate("source('RScripts/sample.R')");

            // .NET Framework array to R vector.
//            NumericVector group1 = REngine.CreateNumericVector(new double[] { 30.02, 29.99, 30.11, 29.97, 30.01, 29.99 });
//            REngine.SetSymbol("group1", group1);
//            // Direct parsing from R script.
//            NumericVector group2 = REngine.Evaluate("group2 <- c(29.89, 29.93, 29.72, 29.98, 30.02, 29.98)").AsNumeric();
//
//            // Test difference of mean and get the P-value.
//            GenericVector testResult = REngine.Evaluate("t.test(group1, group2)").AsList();
//            double p = testResult["p.value"].AsNumeric().First();
//
//            Console.WriteLine("Group1: [{0}]", string.Join(", ", group1));
//            Console.WriteLine("Group2: [{0}]", string.Join(", ", group2));
//            Console.WriteLine("P-value = {0:0.000}", p);
        }
    }
}

