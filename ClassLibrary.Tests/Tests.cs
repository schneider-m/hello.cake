using NUnit.Framework;

namespace ClassLibrary.Tests
{
    public class Tests
    {
        [Test]
        public void HelloWorldTest()
        {
            var sut = new Class1();
            Assert.AreEqual(sut.HelloWorld(), "Hello World!");
        }
    }
}
