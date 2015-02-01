using IntegrationEngine.Model;
using System;

namespace IntegrationEngine.Core.Tests.Storage
{
    public class DocumentStub : IHasStringId
    {
        #region IHasStringId implementation

        public string Id { get; set; }

        #endregion

        public DocumentStub()
        {
        }
    }
}

