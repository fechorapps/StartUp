namespace Application.UnitTests;

public class ExampleTest
{
    [Fact]
    public void Example_ShouldPass()
    {
        // Arrange
        var expected = true;

        // Act
        var actual = true;

        // Assert
        actual.Should().Be(expected);
    }
}
