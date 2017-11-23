using System;

namespace InEngine.Core
{
    public interface IFailed
    {
        void Failed(Exception exception);
    }
}
