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
        var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
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

        await SembrarUsuariosDeTrabajoAsync(
            context, userManager, passwordHasher, configuration, environment, logger);

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

    /// <summary>
    /// Crea las cuentas de trabajo declaradas en <c>Seed:Usuarios:Cuentas</c>.
    /// Solo corre en Development y solo crea lo que falta: nunca toca la contraseña
    /// de una cuenta existente, para que un despliegue no pueda dejar credenciales conocidas.
    /// </summary>
    private static async Task SembrarUsuariosDeTrabajoAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IPasswordHasher<ApplicationUser> passwordHasher,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger logger)
    {
        if (!environment.IsDevelopment())
        {
            return;
        }

        var cuentas = configuration.GetSection("Seed:Usuarios:Cuentas").Get<CuentaSembrada[]>();
        if (cuentas is null || cuentas.Length == 0)
        {
            return;
        }

        var contrasena = configuration["Seed:Usuarios:ContrasenaPredeterminada"];
        if (string.IsNullOrWhiteSpace(contrasena))
        {
            logger.LogWarning(
                "Hay {Cantidad} cuenta(s) en Seed:Usuarios:Cuentas pero falta " +
                "Seed:Usuarios:ContrasenaPredeterminada; se omite el sembrado.",
                cuentas.Length);
            return;
        }

        foreach (var cuenta in cuentas)
        {
            var correoCuenta = cuenta.Email?.Trim();
            if (string.IsNullOrWhiteSpace(correoCuenta))
            {
                continue;
            }

            var existente = await userManager.FindByEmailAsync(correoCuenta);
            if (existente is not null)
            {
                continue;
            }

            var nuevo = new ApplicationUser
            {
                UserName = correoCuenta,
                Email = correoCuenta,
                Nombre = cuenta.Nombre?.Trim() ?? correoCuenta,
                EmailConfirmed = true,
                Activo = true
            };

            // Igual que el supervisor inicial: el hash se calcula aquí para no
            // relajar la política de contraseñas del sistema.
            nuevo.PasswordHash = passwordHasher.HashPassword(nuevo, contrasena);
            VerificarResultado(await userManager.CreateAsync(nuevo), $"crear el usuario {correoCuenta}");

            var rol = string.IsNullOrWhiteSpace(cuenta.Rol) ? "Registrador" : cuenta.Rol.Trim();
            VerificarResultado(
                await userManager.AddToRoleAsync(nuevo, rol),
                $"asignar el rol {rol} a {correoCuenta}");

            await AsignarAmbientePredeterminadoAsync(context, nuevo, cuenta.Ambiente, correoCuenta, logger);
            logger.LogInformation("Se creó la cuenta de desarrollo {Correo} con rol {Rol}.", correoCuenta, rol);
        }

        await context.SaveChangesAsync();
    }

    private static async Task AsignarAmbientePredeterminadoAsync(
        ApplicationDbContext context,
        ApplicationUser usuario,
        string? nombreAmbiente,
        string correo,
        ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(nombreAmbiente))
        {
            return;
        }

        var ambiente = await context.Ambientes
            .SingleOrDefaultAsync(x => x.Nombre == nombreAmbiente.Trim());
        if (ambiente is null)
        {
            logger.LogWarning(
                "No existe el ambiente '{Ambiente}' declarado para {Correo}; se omite la asignación.",
                nombreAmbiente,
                correo);
            return;
        }

        context.UsuariosAmbientes.Add(new UsuarioAmbiente
        {
            UsuarioId = usuario.Id,
            AmbienteId = ambiente.Id,
            EsPredeterminado = true,
            Activo = true
        });
    }

    private sealed class CuentaSembrada
    {
        public string? Email { get; set; }

        public string? Nombre { get; set; }

        public string? Rol { get; set; }

        public string? Ambiente { get; set; }
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
