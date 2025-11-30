// See https://aka.ms/new-console-template for more information
namespace Chunnel.Tests;

public class Tests
{
  [Test]
  public async Task SimpleTest()
  {
    var x = 2 + 2;
    await Assert.That(x).IsEqualTo(4);
  }

  [Test]
  public async Task SimpleTest2()
  {
    var y = 2 * 2;
    await Assert.That(y).IsEqualTo(4);
  }

}