using Nest;
using Nest.Domain;
using System;
using System.Collections.Generic;

namespace IntegrationEngine.Core.Tests.Storage
{
    public class StubHit<TItem> : IHit<TItem> where TItem : class
    {
        public StubHit()
        {
        }

        #region IHit implementation

        public IFieldSelection<TItem> Fields {
            get {
                throw new NotImplementedException();
            }
        }

        public virtual TItem Source {
            get {
                throw new NotImplementedException();
            }
        }

        public string Index {
            get {
                throw new NotImplementedException();
            }
        }

        public double Score {
            get {
                throw new NotImplementedException();
            }
        }

        public string Type {
            get {
                throw new NotImplementedException();
            }
        }

        public string Version {
            get {
                throw new NotImplementedException();
            }
        }

        public virtual string Id {
            get {
                throw new NotImplementedException();
            }
        }

        public System.Collections.Generic.IEnumerable<object> Sorts {
            get {
                throw new NotImplementedException();
            }
        }

        public HighlightFieldDictionary Highlights {
            get {
                throw new NotImplementedException();
            }
        }

        public Explanation Explanation {
            get {
                throw new NotImplementedException();
            }
        }

        public IEnumerable<string> MatchedQueries {
            get {
                throw new NotImplementedException();
            }
        }

        #endregion
    }
}

