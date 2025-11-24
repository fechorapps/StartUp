# Domain Unit Tests

Comprehensive unit tests for DDD base classes following enterprise testing best practices.

## Test Coverage

### Base Classes Tested

1. **Entity&lt;TId&gt;** - `Common/EntityTests.cs`
   - Constructor behavior
   - Identity-based equality
   - Hash code generation
   - Equality operators (==, !=)
   - Date tracking (CreatedOnUtc, ModifiedOnUtc)
   - Collection behavior (HashSet, Dictionary)
   - Edge cases

2. **ValueObject** - `Common/ValueObjectTests.cs`
   - Value-based equality
   - Immutability verification
   - Hash code generation
   - Equality operators
   - Nullable component handling
   - Collection behavior
   - Reference vs value equality

3. **AggregateRoot&lt;TId&gt;** - `Common/AggregateRootTests.cs`
   - Entity inheritance
   - Domain event management
   - Event workflow simulation
   - Event properties validation
   - Aggregate isolation
   - Business logic integration

## Test Organization

```
Domain.UnitTests/
├── Common/
│   ├── EntityTests.cs              (40+ tests)
│   ├── ValueObjectTests.cs         (45+ tests)
│   ├── AggregateRootTests.cs       (35+ tests)
│   └── TestHelpers/
│       ├── TestEntity.cs           (Concrete entity for testing)
│       ├── TestValueObject.cs      (Concrete value objects)
│       └── TestAggregateRoot.cs    (Concrete aggregate root)
├── Domain.UnitTests.csproj
├── Usings.cs
└── README.md
```

## Testing Strategy

### Test Pyramid Alignment
- **Unit Tests (70%)**: Focus on base classes and domain logic
- **Integration Tests (20%)**: Repository and database interactions
- **E2E Tests (10%)**: Full workflow scenarios

### Testing Principles

1. **AAA Pattern**: Arrange-Act-Assert structure
2. **Single Responsibility**: One assertion concept per test
3. **Descriptive Names**: Clear test names indicating behavior
4. **Independence**: Tests can run in any order
5. **Comprehensive Coverage**: Happy paths, edge cases, and error scenarios

### Test Categories

#### Constructor Tests
- Verify proper initialization
- Test default values
- Validate parameter handling

#### Equality Tests
- Identity-based equality (Entity)
- Value-based equality (ValueObject)
- Null handling
- Type comparison
- Reference equality

#### Operator Tests
- Equality operators (==, !=)
- Null handling in operators
- Type safety

#### Hash Code Tests
- Consistency
- Equality contract compliance
- Collection behavior

#### Behavior Tests
- Domain logic
- State changes
- Event generation
- Invariant enforcement

#### Edge Cases
- Empty values
- Null components
- Boundary conditions
- Extreme values

## Test Helpers

### TestEntity
Concrete implementation of `Entity<TId>` for testing:
- Simple entity with Name property
- Exposes UpdateModifiedDate method
- Supports serialization scenarios

### TestValueObject
Concrete value objects for testing:
- **Address**: Multi-component value object
- **Money**: Value object with nullable components
- **EmptyValueObject**: Edge case testing

### TestAggregateRoot
Concrete implementation of `AggregateRoot<TId>` for testing:
- Business operations (ChangeName, PerformAction)
- Domain event generation
- Version tracking

### Test Domain Events
- **TestNameChangedEvent**: Named business event
- **TestActionPerformedEvent**: Generic action event
- **CustomTestEvent**: Flexible test event

## Running Tests

```bash
# Run all domain unit tests
dotnet test tests/Unit/Domain.UnitTests

# Run with coverage
dotnet test tests/Unit/Domain.UnitTests --collect:"XPlat Code Coverage"

# Run specific test class
dotnet test tests/Unit/Domain.UnitTests --filter "FullyQualifiedName~EntityTests"

# Run with detailed output
dotnet test tests/Unit/Domain.UnitTests --verbosity detailed
```

## Test Frameworks

- **xUnit**: Primary testing framework
- **FluentAssertions**: Expressive assertion library
- **coverlet**: Code coverage collection

## Quality Gates

### Coverage Thresholds
- **Line Coverage**: > 90%
- **Branch Coverage**: > 85%
- **Method Coverage**: > 95%

### Performance Benchmarks
- All unit tests should complete in < 5 seconds
- Individual test execution < 100ms

### Test Quality
- No skipped tests in CI/CD
- No flaky tests
- Clear failure messages
- Meaningful test names

## Best Practices

### Naming Convention
```csharp
[Fact]
public void MethodName_StateUnderTest_ExpectedBehavior()
{
    // Arrange
    // Act
    // Assert
}
```

### Assertions
Use FluentAssertions for readable assertions:
```csharp
result.Should().BeTrue();
entity.Id.Should().Be(expectedId);
events.Should().HaveCount(3);
```

### Test Data
- Use meaningful test data
- Avoid magic numbers
- Create constants for shared values

### Isolation
- Each test is independent
- No shared state between tests
- Use test helpers for common setup

## Continuous Improvement

### Future Enhancements
- [ ] Add mutation testing
- [ ] Performance benchmarks
- [ ] Property-based testing
- [ ] Add more edge case scenarios
- [ ] Integration with CI/CD coverage reports

### Metrics to Track
- Code coverage trends
- Test execution time
- Number of tests per class
- Flaky test rate

## Contributing

When adding new tests:
1. Follow the AAA pattern
2. Use descriptive test names
3. Add tests to appropriate test class
4. Ensure tests are independent
5. Update this README if adding new test categories

## References

- [xUnit Documentation](https://xunit.net/)
- [FluentAssertions Documentation](https://fluentassertions.com/)
- [Domain-Driven Design](https://www.domainlanguage.com/ddd/)
- [Testing Best Practices](https://docs.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)
