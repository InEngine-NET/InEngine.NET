using NUnit.Framework;

namespace InEngineTesting;

public abstract class TestBase<TSubject> where TSubject : new()
{
    protected TSubject Subject { get; set; }

    [SetUp]
    public void ConstructSubject() => Subject = new TSubject();
}
