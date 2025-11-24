using ErrorOr;

namespace DoorX.Domain.Common.Errors;

/// <summary>
/// Clase estática que contiene todos los errores de dominio de la aplicación.
/// Organizada por bounded context y tipo de error.
/// </summary>
/// <remarks>
/// Esta clase centraliza la definición de errores usando el patrón ErrorOr.
/// Los errores se organizan en clases estáticas anidadas por bounded context.
///
/// Tipos de Error disponibles:
/// - Error.Validation: Errores de validación de datos
/// - Error.NotFound: Recurso no encontrado
/// - Error.Conflict: Conflicto de estado (ej: operación no permitida en el estado actual)
/// - Error.Failure: Fallo general de operación
/// - Error.Unexpected: Error inesperado del sistema
/// - Error.Forbidden: Operación no autorizada
///
/// Uso:
/// return DomainErrors.ServiceRequest.NotFound;
/// return DomainErrors.ServiceRequest.AlreadyAssigned;
/// </remarks>
public static class DomainErrors
{
    /// <summary>
    /// Errores relacionados con Service Requests (Solicitudes de Servicio).
    /// </summary>
    public static class ServiceRequest
    {
        public static Error NotFound => Error.NotFound(
            code: "ServiceRequest.NotFound",
            description: "La solicitud de servicio no fue encontrada.");

        public static Error AlreadyAssigned => Error.Conflict(
            code: "ServiceRequest.AlreadyAssigned",
            description: "La solicitud de servicio ya tiene un contratista asignado.");

        public static Error CannotAssignWhenCancelled => Error.Conflict(
            code: "ServiceRequest.CannotAssignWhenCancelled",
            description: "No se puede asignar un contratista a una solicitud cancelada.");

        public static Error CannotCancelWhenCompleted => Error.Conflict(
            code: "ServiceRequest.CannotCancelWhenCompleted",
            description: "No se puede cancelar una solicitud que ya fue completada.");

        public static Error InvalidDescription => Error.Validation(
            code: "ServiceRequest.InvalidDescription",
            description: "La descripción de la solicitud es inválida o está vacía.");

        public static Error MaxBidsReached => Error.Conflict(
            code: "ServiceRequest.MaxBidsReached",
            description: "Se ha alcanzado el número máximo de ofertas permitidas.");
    }

    /// <summary>
    /// Errores relacionados con Properties (Propiedades).
    /// </summary>
    public static class Property
    {
        public static Error NotFound => Error.NotFound(
            code: "Property.NotFound",
            description: "La propiedad no fue encontrada.");

        public static Error InvalidAddress => Error.Validation(
            code: "Property.InvalidAddress",
            description: "La dirección de la propiedad es inválida.");

        public static Error AlreadyOccupied => Error.Conflict(
            code: "Property.AlreadyOccupied",
            description: "La propiedad ya está ocupada por otro inquilino.");
    }

    /// <summary>
    /// Errores relacionados con Tenants (Inquilinos).
    /// </summary>
    public static class Tenant
    {
        public static Error NotFound => Error.NotFound(
            code: "Tenant.NotFound",
            description: "El inquilino no fue encontrado.");

        public static Error NotActive => Error.Conflict(
            code: "Tenant.NotActive",
            description: "El inquilino no está activo y no puede crear solicitudes.");

        public static Error InvalidEmail => Error.Validation(
            code: "Tenant.InvalidEmail",
            description: "El correo electrónico del inquilino es inválido.");

        public static Error InvalidPhoneNumber => Error.Validation(
            code: "Tenant.InvalidPhoneNumber",
            description: "El número de teléfono del inquilino es inválido.");
    }

    /// <summary>
    /// Errores relacionados con Vendors/Contractors (Contratistas).
    /// </summary>
    public static class Vendor
    {
        public static Error NotFound => Error.NotFound(
            code: "Vendor.NotFound",
            description: "El contratista no fue encontrado.");

        public static Error NotAvailable => Error.Conflict(
            code: "Vendor.NotAvailable",
            description: "El contratista no está disponible en este momento.");

        public static Error NotQualified => Error.Conflict(
            code: "Vendor.NotQualified",
            description: "El contratista no está calificado para este tipo de servicio.");

        public static Error OutsideServiceArea => Error.Conflict(
            code: "Vendor.OutsideServiceArea",
            description: "La propiedad está fuera del área de servicio del contratista.");

        public static Error DuplicateBid => Error.Conflict(
            code: "Vendor.DuplicateBid",
            description: "El contratista ya ha enviado una oferta para esta solicitud.");
    }

    /// <summary>
    /// Errores relacionados con Landlords (Propietarios).
    /// </summary>
    public static class Landlord
    {
        public static Error NotFound => Error.NotFound(
            code: "Landlord.NotFound",
            description: "El propietario no fue encontrado.");

        public static Error InvalidContact => Error.Validation(
            code: "Landlord.InvalidContact",
            description: "La información de contacto del propietario es inválida.");
    }

    /// <summary>
    /// Errores relacionados con External Integrations (Integraciones Externas).
    /// </summary>
    public static class Integration
    {
        public static Error ProviderNotConfigured => Error.Failure(
            code: "Integration.ProviderNotConfigured",
            description: "El proveedor de integración no está configurado para esta propiedad.");

        public static Error SyncFailed => Error.Failure(
            code: "Integration.SyncFailed",
            description: "Falló la sincronización con el sistema externo.");

        public static Error ProviderUnavailable => Error.Failure(
            code: "Integration.ProviderUnavailable",
            description: "El proveedor externo no está disponible en este momento.");
    }

    /// <summary>
    /// Errores generales de validación.
    /// </summary>
    public static class General
    {
        public static Error InvalidId => Error.Validation(
            code: "General.InvalidId",
            description: "El identificador proporcionado es inválido.");

        public static Error Required => Error.Validation(
            code: "General.Required",
            description: "El campo es requerido.");

        public static Error UnexpectedError => Error.Unexpected(
            code: "General.UnexpectedError",
            description: "Ocurrió un error inesperado.");

        public static Error Unauthorized => Error.Forbidden(
            code: "General.Unauthorized",
            description: "No tiene permisos para realizar esta operación.");
    }
}
