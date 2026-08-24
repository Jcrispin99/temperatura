# Arquitectura Técnica - Sistema de Registro de Temperatura

**Fecha:** 11-08-2026 | **Stack:** C# .NET | **Patrón:** Clean Architecture + DDD

---

## 1. VALIDACIÓN DEL MODELO NO-CODE ✅

Tu análisis está sólido. Aquí la validación y ajustes:

### Entidades Correctamente Identificadas

✅ **Medicion** - Definición de qué se mide (Temperatura, Humedad, Refrigeración)
✅ **Ambiente** - Lugares físicos (Farmacia, Enfermería, UMA 1/2/3)
✅ **Horario/Turno** - Bloques de tiempo (7am, 1pm, 7pm, 1am)
✅ **Usuario** - Personal que toma registros
✅ **RegistroDiario** - Transacciones diarias (la tabla que crece)

### Tablas de Configuración (Relaciones M:N)

✅ **Ambiente_Medicion** - Qué mide cada ambiente
✅ **Ambiente_Horario** - Qué turnos registra cada ambiente

### Campos Calculados

✅ **Es_Alerta** - Boolean basado en rango
✅ **Porcentaje_Avance** - KPI dinámico por ambiente

---

## 2. ESTRUCTURA DE BASE DE DATOS (SQL Server / PostgreSQL)

```sql
-- Tabla Base: Mediciones
CREATE TABLE Mediciones (
    ID INT PRIMARY KEY IDENTITY(1,1),
    Nombre NVARCHAR(100) NOT NULL UNIQUE, -- "Temperatura", "Humedad", "Refrigeración"
    RangoMinimo DECIMAL(10,2) NOT NULL,
    RangoMaximo DECIMAL(10,2) NOT NULL,
    Unidad NVARCHAR(20), -- "°C", "%", etc.
    Activo BIT DEFAULT 1,
    FechaCreacion DATETIME DEFAULT GETDATE()
);

-- Tabla Base: Ambientes
CREATE TABLE Ambientes (
    ID INT PRIMARY KEY IDENTITY(1,1),
    Nombre NVARCHAR(100) NOT NULL UNIQUE, -- "Farmacia", "Enfermería", "UMA 1", etc.
    Descripcion NVARCHAR(255),
    Activo BIT DEFAULT 1,
    FechaCreacion DATETIME DEFAULT GETDATE()
);

-- Tabla Base: Horarios/Turnos
CREATE TABLE Horarios (
    ID INT PRIMARY KEY IDENTITY(1,1),
    Hora TIME NOT NULL UNIQUE, -- "07:00", "13:00", "19:00", "01:00"
    Nombre NVARCHAR(50), -- "Turno Mañana", "Turno Tarde", etc.
    Activo BIT DEFAULT 1
);

-- Tabla Base: Usuarios
CREATE TABLE Usuarios (
    ID INT PRIMARY KEY IDENTITY(1,1),
    Nombre NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100) UNIQUE,
    Ambiente_ID INT NOT NULL,
    Activo BIT DEFAULT 1,
    FechaCreacion DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (Ambiente_ID) REFERENCES Ambientes(ID)
);

-- Tabla Configuración: Ambiente-Medición
CREATE TABLE AmbienteMedicion (
    ID INT PRIMARY KEY IDENTITY(1,1),
    Ambiente_ID INT NOT NULL,
    Medicion_ID INT NOT NULL,
    Requerido BIT DEFAULT 1, -- Si es obligatorio para ese ambiente
    UNIQUE(Ambiente_ID, Medicion_ID),
    FOREIGN KEY (Ambiente_ID) REFERENCES Ambientes(ID),
    FOREIGN KEY (Medicion_ID) REFERENCES Mediciones(ID)
);

-- Tabla Configuración: Ambiente-Horario
CREATE TABLE AmbienteHorario (
    ID INT PRIMARY KEY IDENTITY(1,1),
    Ambiente_ID INT NOT NULL,
    Horario_ID INT NOT NULL,
    Habilitado BIT DEFAULT 1,
    UNIQUE(Ambiente_ID, Horario_ID),
    FOREIGN KEY (Ambiente_ID) REFERENCES Ambientes(ID),
    FOREIGN KEY (Horario_ID) REFERENCES Horarios(ID)
);

-- Tabla Transaccional: Registros Diarios
CREATE TABLE RegistrosDiarios (
    ID INT PRIMARY KEY IDENTITY(1,1),
    Fecha DATE NOT NULL,
    Ambiente_ID INT NOT NULL,
    Horario_ID INT NOT NULL,
    Usuario_ID INT NOT NULL,
    Medicion_ID INT NOT NULL,
    ValorRegistrado DECIMAL(10,2) NOT NULL,
    Es_Alerta BIT NOT NULL, -- Calculado en el INSERT
    Notas NVARCHAR(255),
    FechaCreacion DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (Ambiente_ID) REFERENCES Ambientes(ID),
    FOREIGN KEY (Horario_ID) REFERENCES Horarios(ID),
    FOREIGN KEY (Usuario_ID) REFERENCES Usuarios(ID),
    FOREIGN KEY (Medicion_ID) REFERENCES Mediciones(ID),
    INDEX IDX_Fecha_Ambiente (Fecha, Ambiente_ID),
    INDEX IDX_Usuario_Fecha (Usuario_ID, Fecha)
);
```

---

## 3. ESTRUCTURA DE CLASES C# .NET

### 3.1 Domain Models (Entities)

```csharp
// Domain/Entities/Medicion.cs
public class Medicion
{
    public int Id { get; set; }
    public string Nombre { get; set; } // "Temperatura"
    public decimal RangoMinimo { get; set; }
    public decimal RangoMaximo { get; set; }
    public string Unidad { get; set; } // "°C"
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }

    // Navigation
    public ICollection<AmbienteMedicion> AmbienteMediciones { get; set; }
    public ICollection<RegistroDiario> RegistrosDiarios { get; set; }

    // Business logic
    public bool EstaEnRango(decimal valor) => valor >= RangoMinimo && valor <= RangoMaximo;
}

// Domain/Entities/Ambiente.cs
public class Ambiente
{
    public int Id { get; set; }
    public string Nombre { get; set; } // "Farmacia", "UMA 1"
    public string Descripcion { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }

    // Navigation
    public ICollection<Usuario> Usuarios { get; set; }
    public ICollection<AmbienteMedicion> AmbienteMediciones { get; set; }
    public ICollection<AmbienteHorario> AmbienteHorarios { get; set; }
    public ICollection<RegistroDiario> RegistrosDiarios { get; set; }

    // Business logic - Devuelve las mediciones que este ambiente debe tomar
    public ICollection<Medicion> ObtenerMedicionesRequeridas()
        => AmbienteMediciones.Where(am => am.Requerido).Select(am => am.Medicion).ToList();

    // Business logic - Devuelve los turnos que este ambiente debe cubrir
    public ICollection<Horario> ObtenerHorariosHabilitados()
        => AmbienteHorarios.Where(ah => ah.Habilitado).Select(ah => ah.Horario).ToList();

    // KPI - Retorna total de registros esperados por día
    public int ObtenerTotalRegistrosEsperados()
        => ObtenerHorariosHabilitados().Count() * ObtenerMedicionesRequeridas().Count();
}

// Domain/Entities/Horario.cs
public class Horario
{
    public int Id { get; set; }
    public TimeOnly Hora { get; set; } // 07:00
    public string Nombre { get; set; } // "Turno Mañana"
    public bool Activo { get; set; }

    // Navigation
    public ICollection<AmbienteHorario> AmbienteHorarios { get; set; }
    public ICollection<RegistroDiario> RegistrosDiarios { get; set; }
}

// Domain/Entities/Usuario.cs
public class Usuario
{
    public int Id { get; set; }
    public string Nombre { get; set; }
    public string Email { get; set; }
    public int Ambiente_ID { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }

    // Navigation
    public Ambiente Ambiente { get; set; }
    public ICollection<RegistroDiario> RegistrosDiarios { get; set; }
}

// Domain/Entities/AmbienteMedicion.cs (Tabla de Configuración)
public class AmbienteMedicion
{
    public int Id { get; set; }
    public int Ambiente_ID { get; set; }
    public int Medicion_ID { get; set; }
    public bool Requerido { get; set; } = true;

    // Navigation
    public Ambiente Ambiente { get; set; }
    public Medicion Medicion { get; set; }
}

// Domain/Entities/AmbienteHorario.cs (Tabla de Configuración)
public class AmbienteHorario
{
    public int Id { get; set; }
    public int Ambiente_ID { get; set; }
    public int Horario_ID { get; set; }
    public bool Habilitado { get; set; } = true;

    // Navigation
    public Ambiente Ambiente { get; set; }
    public Horario Horario { get; set; }
}

// Domain/Entities/RegistroDiario.cs (Tabla Transaccional - Core del negocio)
public class RegistroDiario
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public int Ambiente_ID { get; set; }
    public int Horario_ID { get; set; }
    public int Usuario_ID { get; set; }
    public int Medicion_ID { get; set; }
    public decimal ValorRegistrado { get; set; }
    public bool Es_Alerta { get; set; } // Calculado
    public string Notas { get; set; }
    public DateTime FechaCreacion { get; set; }

    // Navigation
    public Ambiente Ambiente { get; set; }
    public Horario Horario { get; set; }
    public Usuario Usuario { get; set; }
    public Medicion Medicion { get; set; }
}
```

### 3.2 Repository Pattern (Data Layer)

```csharp
// Application/Interfaces/Repositories/IRepository.cs
public interface IRepository<T> where T : class
{
    Task<T> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
}

// Application/Interfaces/Repositories/IRegistroDiarioRepository.cs
public interface IRegistroDiarioRepository : IRepository<RegistroDiario>
{
    Task<IEnumerable<RegistroDiario>> GetByFechaYAmbienteAsync(DateTime fecha, int ambienteId);
    Task<IEnumerable<RegistroDiario>> GetByUsuarioYFechaAsync(int usuarioId, DateTime fecha);
    Task<int> ContarRegistrosPorTurnoAsync(DateTime fecha, int ambienteId, int horarioId);
}

// Application/Interfaces/IUnitOfWork.cs
public interface IUnitOfWork : IDisposable
{
    IRepository<Medicion> Mediciones { get; }
    IRepository<Ambiente> Ambientes { get; }
    IRepository<Horario> Horarios { get; }
    IRepository<Usuario> Usuarios { get; }
    IRegistroDiarioRepository RegistrosDiarios { get; }

    Task<int> SaveChangesAsync();
}
```

### 3.3 Application Services (Business Logic)

```csharp
// Application/Services/PorcentajeAvanceService.cs
public class PorcentajeAvanceService
{
    private readonly IUnitOfWork _unitOfWork;

    public PorcentajeAvanceService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Calcula el porcentaje de avance para un ambiente en una fecha específica.
    /// Fórmula: (Tomas realizadas / Tomas esperadas) * 100
    /// </summary>
    public async Task<decimal> CalcularPorcentajeAvanceAsync(int ambienteId, DateTime fecha)
    {
        var ambiente = await _unitOfWork.Ambientes.GetByIdAsync(ambienteId);

        if (ambiente == null)
            throw new ArgumentException("Ambiente no encontrado");

        // Tomas esperadas = Horarios * Mediciones requeridas
        int tomasEsperadas = ambiente.ObtenerTotalRegistrosEsperados();

        if (tomasEsperadas == 0)
            return 0; // No hay registros configurados

        // Tomas realizadas - Contar registros únicos por turno+medición
        var registros = await _unitOfWork.RegistrosDiarios
            .GetByFechaYAmbienteAsync(fecha, ambienteId);

        var tomasRealizadas = registros
            .GroupBy(r => new { r.Horario_ID, r.Medicion_ID })
            .Count();

        return (decimal)tomasRealizadas / tomasEsperadas * 100;
    }

    /// <summary>
    /// Valida si un turno específico está completo (todas las mediciones requeridas)
    /// </summary>
    public async Task<bool> TurnoCompletoAsync(int ambienteId, int horarioId, DateTime fecha)
    {
        var ambiente = await _unitOfWork.Ambientes.GetByIdAsync(ambienteId);
        var medicionesRequeridas = ambiente.ObtieneMedicionesRequeridas().Count();

        int registrosEnTurno = await _unitOfWork.RegistrosDiarios
            .ContarRegistrosPorTurnoAsync(fecha, ambienteId, horarioId);

        return registrosEnTurno >= medicionesRequeridas;
    }
}

// Application/Services/RegistroTemperaturaService.cs
public class RegistroTemperaturaService
{
    private readonly IUnitOfWork _unitOfWork;

    public RegistroTemperaturaService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Registra una medición y calcula automáticamente si está en alerta
    /// </summary>
    public async Task<RegistroDiario> RegistrarMedicionAsync(
        DateTime fecha,
        int ambienteId,
        int horarioId,
        int usuarioId,
        int medicionId,
        decimal valorRegistrado,
        string notas = null)
    {
        // Validaciones
        var medicion = await _unitOfWork.Mediciones.GetByIdAsync(medicionId);
        var usuario = await _unitOfWork.Usuarios.GetByIdAsync(usuarioId);
        var ambiente = await _unitOfWork.Ambientes.GetByIdAsync(ambienteId);

        if (medicion == null || usuario == null || ambiente == null)
            throw new ArgumentException("Datos inválidos");

        // Verificar que el usuario esté asignado al ambiente correcto
        if (usuario.Ambiente_ID != ambienteId)
            throw new UnauthorizedAccessException("Usuario no asignado a este ambiente");

        // Crear registro
        var registro = new RegistroDiario
        {
            Fecha = fecha,
            Ambiente_ID = ambienteId,
            Horario_ID = horarioId,
            Usuario_ID = usuarioId,
            Medicion_ID = medicionId,
            ValorRegistrado = valorRegistrado,
            Es_Alerta = !medicion.EstaEnRango(valorRegistrado), // Lógica de alerta
            Notas = notas,
            FechaCreacion = DateTime.UtcNow
        };

        await _unitOfWork.RegistrosDiarios.AddAsync(registro);
        await _unitOfWork.SaveChangesAsync();

        return registro;
    }
}
```

---

## 4. ESTRUCTURA DEL PROYECTO .NET

```
TemperaturaApp/
├── src/
│   ├── Domain/
│   │   ├── Entities/
│   │   │   ├── Medicion.cs
│   │   │   ├── Ambiente.cs
│   │   │   ├── Horario.cs
│   │   │   ├── Usuario.cs
│   │   │   ├── AmbienteMedicion.cs
│   │   │   ├── AmbienteHorario.cs
│   │   │   └── RegistroDiario.cs
│   │   └── Interfaces/
│   │       └── (ValueObjects, Specifications, Events si es necesario)
│   │
│   ├── Application/
│   │   ├── Interfaces/
│   │   │   ├── Repositories/
│   │   │   │   ├── IRepository.cs
│   │   │   │   └── IRegistroDiarioRepository.cs
│   │   │   └── IUnitOfWork.cs
│   │   ├── Services/
│   │   │   ├── PorcentajeAvanceService.cs
│   │   │   └── RegistroTemperaturaService.cs
│   │   ├── DTOs/
│   │   │   ├── RegistroDTO.cs
│   │   │   └── AmbienteDTO.cs
│   │   └── Validators/
│   │       └── RegistroValidator.cs (FluentValidation)
│   │
│   ├── Infrastructure/
│   │   ├── Data/
│   │   │   ├── TemperaturaDbContext.cs
│   │   │   ├── Repositories/
│   │   │   │   ├── Repository.cs
│   │   │   │   └── RegistroDiarioRepository.cs
│   │   │   ├── UnitOfWork.cs
│   │   │   └── Migrations/
│   │   └── DependencyInjection.cs
│   │
│   └── Web/
│       ├── Controllers/
│       │   ├── RegistrosController.cs
│       │   ├── AmbientesController.cs
│       │   └── KPIController.cs
│       ├── Program.cs
│       └── appsettings.json
│
└── tests/
    ├── Application.Tests/
    │   └── PorcentajeAvanceServiceTests.cs
    └── Infrastructure.Tests/
        └── RegistroDiarioRepositoryTests.cs
```

---

## 5. CONFIGURACIÓN INICIAL DATA

```csharp
// Infrastructure/Data/Seeding/DataSeeder.cs
public static class DataSeeder
{
    public static async Task SeedAsync(TemperaturaDbContext context)
    {
        // Mediciones
        var mediciones = new List<Medicion>
        {
            new { Id = 1, Nombre = "Temperatura", RangoMinimo = 15, RangoMaximo = 30, Unidad = "°C" },
            new { Id = 2, Nombre = "Humedad", RangoMinimo = 15, RangoMaximo = 65, Unidad = "%" },
            new { Id = 3, Nombre = "Refrigeración", RangoMinimo = 2, RangoMaximo = 8, Unidad = "°C" }
        };

        // Ambientes
        var ambientes = new List<Ambiente>
        {
            new { Id = 1, Nombre = "Farmacia", Descripcion = "Área de medicinas" },
            new { Id = 2, Nombre = "Enfermería", Descripcion = "Área de cuidado" },
            new { Id = 3, Nombre = "UMA 1", Descripcion = "Unidad de Medicina A" },
            new { Id = 4, Nombre = "UMA 2", Descripcion = "Unidad de Medicina B" },
            new { Id = 5, Nombre = "UMA 3", Descripcion = "Unidad de Medicina C" }
        };

        // Horarios (Turnos)
        var horarios = new List<Horario>
        {
            new { Id = 1, Hora = new TimeOnly(07, 0), Nombre = "Turno Mañana" },
            new { Id = 2, Hora = new TimeOnly(13, 0), Nombre = "Turno Mediodía" },
            new { Id = 3, Hora = new TimeOnly(19, 0), Nombre = "Turno Tarde" },
            new { Id = 4, Hora = new TimeOnly(01, 0), Nombre = "Turno Noche" }
        };

        // Configuración: Ambiente-Medición
        var ambienteMediciones = new List<AmbienteMedicion>
        {
            // UMA 1: Las 3 mediciones
            new { Ambiente_ID = 3, Medicion_ID = 1, Requerido = true },
            new { Ambiente_ID = 3, Medicion_ID = 2, Requerido = true },
            new { Ambiente_ID = 3, Medicion_ID = 3, Requerido = true },

            // Farmacia: Las 3 mediciones
            new { Ambiente_ID = 1, Medicion_ID = 1, Requerido = true },
            new { Ambiente_ID = 1, Medicion_ID = 2, Requerido = true },
            new { Ambiente_ID = 1, Medicion_ID = 3, Requerido = true },

            // Enfermería: Solo Refrigeración
            new { Ambiente_ID = 2, Medicion_ID = 3, Requerido = true },

            // UMA 2: Las 3 mediciones
            new { Ambiente_ID = 4, Medicion_ID = 1, Requerido = true },
            new { Ambiente_ID = 4, Medicion_ID = 2, Requerido = true },
            new { Ambiente_ID = 4, Medicion_ID = 3, Requerido = true },

            // UMA 3: Las 3 mediciones
            new { Ambiente_ID = 5, Medicion_ID = 1, Requerido = true },
            new { Ambiente_ID = 5, Medicion_ID = 2, Requerido = true },
            new { Ambiente_ID = 5, Medicion_ID = 3, Requerido = true }
        };

        // Configuración: Ambiente-Horario
        var ambienteHorarios = new List<AmbienteHorario>
        {
            // UMA 1: 4 turnos (7am, 1pm, 7pm, 1am)
            new { Ambiente_ID = 3, Horario_ID = 1, Habilitado = true },
            new { Ambiente_ID = 3, Horario_ID = 2, Habilitado = true },
            new { Ambiente_ID = 3, Horario_ID = 3, Habilitado = true },
            new { Ambiente_ID = 3, Horario_ID = 4, Habilitado = true },

            // Farmacia: 3 turnos (7am, 1pm, 7pm - SIN noche)
            new { Ambiente_ID = 1, Horario_ID = 1, Habilitado = true },
            new { Ambiente_ID = 1, Horario_ID = 2, Habilitado = true },
            new { Ambiente_ID = 1, Horario_ID = 3, Habilitado = true },

            // Enfermería: 4 turnos
            new { Ambiente_ID = 2, Horario_ID = 1, Habilitado = true },
            new { Ambiente_ID = 2, Horario_ID = 2, Habilitado = true },
            new { Ambiente_ID = 2, Horario_ID = 3, Habilitado = true },
            new { Ambiente_ID = 2, Horario_ID = 4, Habilitado = true },

            // UMA 2: 4 turnos
            new { Ambiente_ID = 4, Horario_ID = 1, Habilitado = true },
            new { Ambiente_ID = 4, Horario_ID = 2, Habilitado = true },
            new { Ambiente_ID = 4, Horario_ID = 3, Habilitado = true },
            new { Ambiente_ID = 4, Horario_ID = 4, Habilitado = true },

            // UMA 3: 3 turnos (7am, 1pm, 7pm - SIN noche)
            new { Ambiente_ID = 5, Horario_ID = 1, Habilitado = true },
            new { Ambiente_ID = 5, Horario_ID = 2, Habilitado = true },
            new { Ambiente_ID = 5, Horario_ID = 3, Habilitado = true }
        };

        // Insertar datos...
    }
}
```

---

## 6. API REST INICIAL

### Endpoints Core

```
POST   /api/registros              → Crear nuevo registro
GET    /api/registros/:id          → Obtener registro
GET    /api/registros/fecha/:fecha → Obtener registros del día
GET    /api/registros/alerta       → Obtener registros en alerta
GET    /api/ambientes/:id/kpi      → % de avance del ambiente
GET    /api/ambientes/:id/config   → Configuración del ambiente
POST   /api/ambientes              → Crear ambiente
POST   /api/mediciones             → Crear medición
```

---

## 7. PRÓXIMOS PASOS (PLAN DE DESARROLLO)

### Fase 1: Setup Inicial ✓

- [ ] Crear solución .NET
- [ ] Setup EF Core + Migraciones
- [ ] Implementar DbContext
- [ ] Seed de datos iniciales

### Fase 2: Data Layer

- [ ] Implementar Repositories
- [ ] Implementar Unit of Work
- [ ] Tests de repositories

### Fase 3: Business Logic

- [ ] Services (Registro, KPI)
- [ ] Validaciones (FluentValidation)
- [ ] Tests unitarios

### Fase 4: API

- [ ] Controllers
- [ ] DTOs + Mappers
- [ ] Autorización (si es necesario)
- [ ] Documentación Swagger

### Fase 5: Frontend (según lo que necesites)

- [ ] Dashboard
- [ ] Registro diario
- [ ] Reportes

---

## 8. PUNTOS CLAVE A RECORDAR

✅ **Escalabilidad:** Añadir UMA 4 o nuevo horario = solo agregar datos en tablas config
✅ **Excepciones:** Enfermería (1 medición) y Farmacia/UMA3 (3 turnos) se resuelven con AmbienteMedicion y AmbienteHorario
✅ **KPI Dinámico:** Cada ambiente tiene diferente expectativa de registros
✅ **Alertas Automáticas:** Calculadas en la creación del registro
✅ **Auditoría:** FechaCreacion en todas las transacciones

---

**¿Vamos con el Setup inicial del proyecto?**
