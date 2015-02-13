using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using Nest;
using IntegrationEngine.Core.Reports;

namespace IntegrationEngine.Core.R
{
    public interface IRScriptRunner
    {
        IElasticClient ElasticClient { get; set; }
        void Run<T>(IReport<T> report);
    }
}

