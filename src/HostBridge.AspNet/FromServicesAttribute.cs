namespace HostBridge.AspNet;

/// <summary>
/// Marks an ASP.NET Web Forms or handler property to be populated from the current request's
/// dependency injection scope.
/// </summary>
/// <remarks>
/// Properties with this attribute are set by <see cref="AspNetRequestScopeModule"/> during
/// <c>PreRequestHandlerExecute</c>. The value is resolved from the current request's
/// <see cref="Microsoft.Extensions.DependencyInjection.IServiceScope"/>.
/// </remarks>
/// <example>
/// public partial class Default : System.Web.UI.Page {
///     [FromServices] public IClock Clock { get; set; } = default!;
/// }
/// </example>
[AttributeUsage(AttributeTargets.Property)]
public sealed class FromServicesAttribute : Attribute {}