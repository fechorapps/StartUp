using DoorX.Domain.Common.Errors;
using ErrorOr;

namespace Domain.UnitTests.Common;

/// <summary>
/// Unit tests for DomainErrors static class.
/// Ensures all error definitions are properly configured.
/// </summary>
public class DomainErrorsTests
{
    #region ServiceRequest Errors

    [Fact]
    public void ServiceRequest_NotFound_ShouldReturnNotFoundError()
    {
        // Act
        var error = DomainErrors.ServiceRequest.NotFound;

        // Assert
        error.Type.Should().Be(ErrorType.NotFound);
        error.Code.Should().Be("ServiceRequest.NotFound");
        error.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ServiceRequest_AlreadyAssigned_ShouldReturnConflictError()
    {
        // Act
        var error = DomainErrors.ServiceRequest.AlreadyAssigned;

        // Assert
        error.Type.Should().Be(ErrorType.Conflict);
        error.Code.Should().Be("ServiceRequest.AlreadyAssigned");
        error.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ServiceRequest_CannotAssignWhenCancelled_ShouldReturnConflictError()
    {
        // Act
        var error = DomainErrors.ServiceRequest.CannotAssignWhenCancelled;

        // Assert
        error.Type.Should().Be(ErrorType.Conflict);
        error.Code.Should().Be("ServiceRequest.CannotAssignWhenCancelled");
        error.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ServiceRequest_CannotCancelWhenCompleted_ShouldReturnConflictError()
    {
        // Act
        var error = DomainErrors.ServiceRequest.CannotCancelWhenCompleted;

        // Assert
        error.Type.Should().Be(ErrorType.Conflict);
        error.Code.Should().Be("ServiceRequest.CannotCancelWhenCompleted");
        error.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ServiceRequest_InvalidDescription_ShouldReturnValidationError()
    {
        // Act
        var error = DomainErrors.ServiceRequest.InvalidDescription;

        // Assert
        error.Type.Should().Be(ErrorType.Validation);
        error.Code.Should().Be("ServiceRequest.InvalidDescription");
        error.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ServiceRequest_MaxBidsReached_ShouldReturnConflictError()
    {
        // Act
        var error = DomainErrors.ServiceRequest.MaxBidsReached;

        // Assert
        error.Type.Should().Be(ErrorType.Conflict);
        error.Code.Should().Be("ServiceRequest.MaxBidsReached");
        error.Description.Should().NotBeNullOrWhiteSpace();
    }

    #endregion

    #region Property Errors

    [Fact]
    public void Property_NotFound_ShouldReturnNotFoundError()
    {
        // Act
        var error = DomainErrors.Property.NotFound;

        // Assert
        error.Type.Should().Be(ErrorType.NotFound);
        error.Code.Should().Be("Property.NotFound");
        error.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Property_InvalidAddress_ShouldReturnValidationError()
    {
        // Act
        var error = DomainErrors.Property.InvalidAddress;

        // Assert
        error.Type.Should().Be(ErrorType.Validation);
        error.Code.Should().Be("Property.InvalidAddress");
        error.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Property_AlreadyOccupied_ShouldReturnConflictError()
    {
        // Act
        var error = DomainErrors.Property.AlreadyOccupied;

        // Assert
        error.Type.Should().Be(ErrorType.Conflict);
        error.Code.Should().Be("Property.AlreadyOccupied");
        error.Description.Should().NotBeNullOrWhiteSpace();
    }

    #endregion

    #region Tenant Errors

    [Fact]
    public void Tenant_NotFound_ShouldReturnNotFoundError()
    {
        // Act
        var error = DomainErrors.Tenant.NotFound;

        // Assert
        error.Type.Should().Be(ErrorType.NotFound);
        error.Code.Should().Be("Tenant.NotFound");
        error.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Tenant_NotActive_ShouldReturnConflictError()
    {
        // Act
        var error = DomainErrors.Tenant.NotActive;

        // Assert
        error.Type.Should().Be(ErrorType.Conflict);
        error.Code.Should().Be("Tenant.NotActive");
        error.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Tenant_InvalidEmail_ShouldReturnValidationError()
    {
        // Act
        var error = DomainErrors.Tenant.InvalidEmail;

        // Assert
        error.Type.Should().Be(ErrorType.Validation);
        error.Code.Should().Be("Tenant.InvalidEmail");
        error.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Tenant_InvalidPhoneNumber_ShouldReturnValidationError()
    {
        // Act
        var error = DomainErrors.Tenant.InvalidPhoneNumber;

        // Assert
        error.Type.Should().Be(ErrorType.Validation);
        error.Code.Should().Be("Tenant.InvalidPhoneNumber");
        error.Description.Should().NotBeNullOrWhiteSpace();
    }

    #endregion

    #region Vendor Errors

    [Fact]
    public void Vendor_NotFound_ShouldReturnNotFoundError()
    {
        // Act
        var error = DomainErrors.Vendor.NotFound;

        // Assert
        error.Type.Should().Be(ErrorType.NotFound);
        error.Code.Should().Be("Vendor.NotFound");
        error.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Vendor_NotAvailable_ShouldReturnConflictError()
    {
        // Act
        var error = DomainErrors.Vendor.NotAvailable;

        // Assert
        error.Type.Should().Be(ErrorType.Conflict);
        error.Code.Should().Be("Vendor.NotAvailable");
        error.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Vendor_NotQualified_ShouldReturnConflictError()
    {
        // Act
        var error = DomainErrors.Vendor.NotQualified;

        // Assert
        error.Type.Should().Be(ErrorType.Conflict);
        error.Code.Should().Be("Vendor.NotQualified");
        error.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Vendor_OutsideServiceArea_ShouldReturnConflictError()
    {
        // Act
        var error = DomainErrors.Vendor.OutsideServiceArea;

        // Assert
        error.Type.Should().Be(ErrorType.Conflict);
        error.Code.Should().Be("Vendor.OutsideServiceArea");
        error.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Vendor_DuplicateBid_ShouldReturnConflictError()
    {
        // Act
        var error = DomainErrors.Vendor.DuplicateBid;

        // Assert
        error.Type.Should().Be(ErrorType.Conflict);
        error.Code.Should().Be("Vendor.DuplicateBid");
        error.Description.Should().NotBeNullOrWhiteSpace();
    }

    #endregion

    #region Landlord Errors

    [Fact]
    public void Landlord_NotFound_ShouldReturnNotFoundError()
    {
        // Act
        var error = DomainErrors.Landlord.NotFound;

        // Assert
        error.Type.Should().Be(ErrorType.NotFound);
        error.Code.Should().Be("Landlord.NotFound");
        error.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Landlord_InvalidContact_ShouldReturnValidationError()
    {
        // Act
        var error = DomainErrors.Landlord.InvalidContact;

        // Assert
        error.Type.Should().Be(ErrorType.Validation);
        error.Code.Should().Be("Landlord.InvalidContact");
        error.Description.Should().NotBeNullOrWhiteSpace();
    }

    #endregion

    #region Integration Errors

    [Fact]
    public void Integration_ProviderNotConfigured_ShouldReturnFailureError()
    {
        // Act
        var error = DomainErrors.Integration.ProviderNotConfigured;

        // Assert
        error.Type.Should().Be(ErrorType.Failure);
        error.Code.Should().Be("Integration.ProviderNotConfigured");
        error.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Integration_SyncFailed_ShouldReturnFailureError()
    {
        // Act
        var error = DomainErrors.Integration.SyncFailed;

        // Assert
        error.Type.Should().Be(ErrorType.Failure);
        error.Code.Should().Be("Integration.SyncFailed");
        error.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Integration_ProviderUnavailable_ShouldReturnFailureError()
    {
        // Act
        var error = DomainErrors.Integration.ProviderUnavailable;

        // Assert
        error.Type.Should().Be(ErrorType.Failure);
        error.Code.Should().Be("Integration.ProviderUnavailable");
        error.Description.Should().NotBeNullOrWhiteSpace();
    }

    #endregion

    #region General Errors

    [Fact]
    public void General_InvalidId_ShouldReturnValidationError()
    {
        // Act
        var error = DomainErrors.General.InvalidId;

        // Assert
        error.Type.Should().Be(ErrorType.Validation);
        error.Code.Should().Be("General.InvalidId");
        error.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void General_Required_ShouldReturnValidationError()
    {
        // Act
        var error = DomainErrors.General.Required;

        // Assert
        error.Type.Should().Be(ErrorType.Validation);
        error.Code.Should().Be("General.Required");
        error.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void General_UnexpectedError_ShouldReturnUnexpectedError()
    {
        // Act
        var error = DomainErrors.General.UnexpectedError;

        // Assert
        error.Type.Should().Be(ErrorType.Unexpected);
        error.Code.Should().Be("General.UnexpectedError");
        error.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void General_Unauthorized_ShouldReturnForbiddenError()
    {
        // Act
        var error = DomainErrors.General.Unauthorized;

        // Assert
        error.Type.Should().Be(ErrorType.Forbidden);
        error.Code.Should().Be("General.Unauthorized");
        error.Description.Should().NotBeNullOrWhiteSpace();
    }

    #endregion

    #region Multiple Access Tests

    [Fact]
    public void Errors_AccessedMultipleTimes_ShouldReturnConsistentValues()
    {
        // Arrange & Act
        var firstAccess = DomainErrors.ServiceRequest.NotFound;
        var secondAccess = DomainErrors.ServiceRequest.NotFound;

        // Assert
        firstAccess.Code.Should().Be(secondAccess.Code);
        firstAccess.Description.Should().Be(secondAccess.Description);
        firstAccess.Type.Should().Be(secondAccess.Type);
    }

    #endregion
}
