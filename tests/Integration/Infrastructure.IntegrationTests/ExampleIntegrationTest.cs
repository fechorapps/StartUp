namespace Infrastructure.IntegrationTests;

public class ExampleIntegrationTest
{
    [Fact]
    public void Example_IntegrationTest_ShouldPass()
    {
        // Arrange
        var expected = true;

        // Act
        var actual = true;

        // Assert
        actual.Should().Be(expected);
    }
}
