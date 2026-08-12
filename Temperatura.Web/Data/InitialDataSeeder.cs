using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Temperatura.Web.Domain;

namespace Temperatura.Web.Data;

public static class InitialDataSeeder
{
    private static readonly string[] RolesIniciales = ["Registrador", "Supervisor"];

    public static async Task InicializarDatosAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<ApplicationUser>>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("InitialDataSeeder");

        await context.Database.MigrateAsync();

        foreach (var nombreRol in RolesIniciales)
        {
            if (!await roleManager.RoleExistsAsync(nombreRol))
            {
                VerificarResultado(
                    await roleManager.CreateAsync(new IdentityRole(nombreRol)),
                    $"crear el rol {nombreRol}");
            }
        }

        var correo = configuration["Seed:Supervisor:Email"]?.Trim();
        if (string.IsNullOrWhiteSpace(correo))
        {
            logger.LogInformation("No se configuró Seed:Supervisor:Email; se omite el supervisor inicial.");
            return;
        }

        var supervisor = await userManager.FindByEmailAsync(correo);
        if (supervisor is null)
        {
            var contrasena = configuration["Seed:Supervisor:Password"];
            if (string.IsNullOrWhiteSpace(contrasena))
            {
                throw new InvalidOperationException(
                    "Configura Seed:Supervisor:Password mediante user-secrets o una variable de entorno para crear el supervisor inicial.");
            }

            supervisor = new ApplicationUser
            {
                UserName = correo,
                Email = correo,
                Nombre = configuration["Seed:Supervisor:Nombre"]?.Trim() ?? "Supervisor",
                EmailConfirmed = true,
                Activo = true
            };

            // La contraseña inicial se recibe desde un secreto y se almacena únicamente como hash.
            // Se asigna directamente para no relajar la política general de contraseñas del sistema.
            supervisor.PasswordHash = passwordHasher.HashPassword(supervisor, contrasena);
            VerificarResultado(await userManager.CreateAsync(supervisor), "crear el supervisor inicial");
            logger.LogInformation("Se creó el supervisor inicial {SupervisorEmail}.", correo);
        }

        if (!await userManager.IsInRoleAsync(supervisor, "Supervisor"))
        {
            VerificarResultado(
                await userManager.AddToRoleAsync(supervisor, "Supervisor"),
                "asignar el rol Supervisor al usuario inicial");
        }
    }

    private static void VerificarResultado(IdentityResult resultado, string operacion)
    {
        if (resultado.Succeeded)
        {
            return;
        }

        var errores = string.Join("; ", resultado.Errors.Select(x => x.Description));
        throw new InvalidOperationException($"No se pudo {operacion}: {errores}");
    }
}
