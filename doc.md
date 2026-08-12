# Sistema de control de temperatura y humedad

## 1. Objetivo

Construir una aplicación para registrar y supervisar las mediciones realizadas en los ambientes del establecimiento.

El sistema debe permitir configurar:

- Los ambientes existentes.
- Las mediciones requeridas en cada ambiente.
- Los rangos permitidos por ambiente y tipo de medición.
- Los horarios que debe cumplir cada ambiente.
- Los usuarios autorizados para registrar en cada ambiente.
- El porcentaje de avance diario de cada ambiente.

La primera versión se desarrollará con C# y .NET.

## 2. Alcance inicial

### Ambientes iniciales

- Farmacia.
- Enfermería.
- UMA 1.
- UMA 2.
- UMA 3.

Los ambientes deben ser configurables. Agregar un ambiente nuevo, como UMA 4, no debe requerir cambios en el código.

### Tipos de medición iniciales

- Temperatura ambiental, expresada en grados Celsius.
- Humedad relativa, expresada en porcentaje.
- Temperatura de refrigeración, expresada en grados Celsius.

Aunque temperatura ambiental y temperatura de refrigeración usan la misma unidad, se consideran tipos de medición diferentes porque representan condiciones distintas.

### Configuración inicial de mediciones

Como regla general, un ambiente puede requerir los tres tipos de medición. También puede configurarse para utilizar solamente algunos.

Configuración inicial para el MVP:

| Ambiente | Temperatura ambiental | Humedad relativa | Temperatura de refrigeración |
| --- | :---: | :---: | :---: |
| Farmacia | Sí | Sí | Sí |
| Enfermería | No | No | Sí |
| UMA 1 | Sí | Sí | Sí |
| UMA 2 | Sí | Sí | Sí |
| UMA 3 | Sí | Sí | Sí |

Esta configuración es un dato inicial modificable y no una regla fija en el código.

### Horarios disponibles

- 07:00.
- 12:00.
- 19:00.
- 00:00.

Ejemplos:

- UMA 1 puede tener cuatro registros diarios.
- Farmacia tiene tres registros: 07:00, 12:00 y 19:00.
- UMA 3 tiene tres registros: 07:00, 12:00 y 19:00.

Los horarios se asignan por ambiente y no deben programarse directamente en el código.

## 3. Día operativo

El primer registro del día operativo es el de las 07:00.

El registro de las 00:00 es el último registro del día operativo anterior. Por ejemplo:

- Día operativo: 11 de agosto de 2026.
- Primer horario: 11 de agosto de 2026 a las 07:00.
- Último horario: 12 de agosto de 2026 a las 00:00.

Para evitar ambigüedad, cada registro tendrá dos datos diferentes:

- `FechaOperativa`: día al que pertenece el cumplimiento.
- `FechaHoraRegistro`: fecha y hora reales en las que el usuario guardó el registro.

## 4. Ventana de registro y registros tardíos

Los horarios representan momentos de referencia; no exigen que el registro se realice en el minuto exacto.

Cada asignación de horario a un ambiente tendrá una ventana configurable:

- Apertura inicial: 30 minutos antes de la hora de referencia.
- Cierre inicial: 1 hora después de la hora de referencia.

Estos valores son la configuración inicial del MVP y podrán modificarse posteriormente sin cambiar el código. También será posible definir ventanas diferentes por ambiente y horario.

El estado de puntualidad se determina de esta forma:

- Antes de 30 minutos previos al horario: registro bloqueado.
- Desde 30 minutos antes y hasta la hora de referencia inclusive: registro permitido y puntual.
- Después de la hora de referencia y antes de cumplirse una hora: registro permitido y tardío.
- Al cumplirse una hora después del horario: registro bloqueado.

Un registro tardío sigue siendo válido y cuenta para completar el avance diario. Una vez cerrada la ventana no se podrá crear el registro y el horario quedará pendiente o incumplido.

Ejemplo para el horario de las 07:00:

| Hora de intento | Resultado |
| --- | --- |
| 06:29 | Bloqueado por anticipación |
| 06:30 a 07:00 | Permitido y puntual |
| Después de 07:00 y antes de 08:00 | Permitido y tardío |
| 08:00 en adelante | Bloqueado por cierre |

## 5. Usuarios y supervisión

Un usuario puede estar asociado con uno o varios ambientes.

- Cada usuario tendrá un ambiente predeterminado.
- El ambiente predeterminado se seleccionará automáticamente al iniciar un registro.
- El usuario podrá registrar en los demás ambientes que tenga asignados.
- Un usuario común solo podrá consultar y registrar en sus ambientes autorizados.
- Un supervisor podrá visualizar el avance y los registros de todos los ambientes.

El modelo detallado de roles y permisos del supervisor se definirá posteriormente. Para el MVP se contemplan conceptualmente los roles `Registrador` y `Supervisor`.

## 6. Rangos permitidos

Los rangos no son globales ni necesariamente iguales para todos los ambientes. Deben configurarse para cada combinación de ambiente y tipo de medición.

Ejemplo conceptual:

| Ambiente | Medición | Mínimo | Máximo | Unidad |
| --- | --- | ---: | ---: | --- |
| UMA 1 | Temperatura ambiental | Por definir | Por definir | °C |
| UMA 1 | Humedad relativa | Por definir | Por definir | % |
| Enfermería | Temperatura de refrigeración | Por definir | Por definir | °C |

Para facilitar el desarrollo y las pruebas, se cargarán temporalmente los siguientes valores demostrativos en todos los ambientes que tengan habilitada la medición:

| Medición | Mínimo demostrativo | Máximo demostrativo | Unidad |
| --- | ---: | ---: | --- |
| Temperatura ambiental | 18 | 26 | °C |
| Humedad relativa | 30 | 70 | % |
| Temperatura de refrigeración | 2 | 8 | °C |

Estos valores no representan una recomendación clínica ni normativa. Son datos de prueba y deberán ser reemplazados por el responsable antes de utilizar el sistema en una operación real.

Los límites son inclusivos: un valor igual al mínimo o al máximo se considera dentro del rango.

Cuando un valor esté fuera de rango:

- El sistema lo marcará para fines visuales y de reporte.
- El usuario podrá guardar normalmente.
- El registro seguirá considerándose completo.
- En esta primera versión no se solicitará una acción correctiva ni una observación obligatoria.

Los cambios futuros de rango no deben modificar el resultado histórico de registros anteriores. Por eso cada detalle conservará los límites que se utilizaron al momento de evaluarlo.

## 7. Modelo conceptual

### Usuario

Persona que ingresa al sistema para registrar o supervisar información.

Datos principales:

- Identificador.
- Nombre.
- Credenciales de acceso.
- Estado activo.

### Rol

Define el nivel general de acceso del usuario.

Roles iniciales previstos:

- Registrador.
- Supervisor.

### Ambiente

Lugar físico donde se realizan las mediciones.

Datos principales:

- Identificador.
- Nombre.
- Estado activo.

### UsuarioAmbiente

Asigna los ambientes en los que un usuario puede trabajar.

Datos principales:

- Usuario.
- Ambiente.
- Indicador de ambiente predeterminado.
- Estado activo.

Reglas:

- Un usuario puede tener varios ambientes activos.
- Solo uno puede ser el ambiente predeterminado.

### TipoMedicion

Catálogo que indica qué se está midiendo.

Datos principales:

- Identificador.
- Nombre.
- Unidad de medida.
- Cantidad de decimales permitidos.
- Estado activo.

### AmbienteMedicion

Define las mediciones requeridas y sus rangos en un ambiente.

Datos principales:

- Ambiente.
- Tipo de medición.
- Rango mínimo.
- Rango máximo.
- Fecha de inicio de vigencia.
- Fecha opcional de fin de vigencia.
- Estado activo.

### Horario

Catálogo de horarios de referencia disponibles.

Datos principales:

- Identificador.
- Nombre descriptivo.
- Hora de referencia.
- Indicador de que pertenece al cierre del día operativo anterior.
- Estado activo.

### AmbienteHorario

Define qué horarios debe cumplir cada ambiente.

Datos principales:

- Ambiente.
- Horario.
- Minutos permitidos antes de la hora de referencia.
- Minutos permitidos después de la hora de referencia.
- Fecha de inicio de vigencia.
- Fecha opcional de fin de vigencia.
- Estado activo.

### Registro

Representa una toma correspondiente a un ambiente, día operativo y horario.

Datos principales:

- Identificador.
- Fecha operativa.
- Ambiente.
- Horario.
- Usuario que registró.
- Fecha y hora real del registro.
- Estado de completitud.
- Indicador de registro tardío.

Reglas:

- Solo puede existir un registro confirmado por ambiente, fecha operativa y horario.
- Un registro confirmado no puede modificarse ni eliminarse en el MVP.
- No se contemplan correcciones ni conservación de versiones anteriores en el MVP.

### DetalleRegistro

Contiene cada valor medido dentro de un registro.

Datos principales:

- Registro.
- Tipo de medición.
- Valor registrado.
- Límite mínimo aplicado.
- Límite máximo aplicado.
- Estado del rango.

Estados de rango previstos:

- Dentro de rango.
- Por debajo del rango.
- Por encima del rango.

### Captura manual

El sistema no se conectará ni controlará el termohigrómetro.

El proceso será completamente manual:

1. El personal consulta físicamente el valor mostrado por el termohigrómetro.
2. El personal selecciona el ambiente y horario correspondiente.
3. El personal transcribe los valores en el sistema.
4. El sistema valida los campos y compara cada valor con su rango configurado.

El dispositivo no será una entidad del MVP.

## 8. Registro completo

Una toma se considera completa cuando:

1. Contiene un valor para cada medición activa requerida por el ambiente.
2. Todos los valores tienen un formato numérico válido.
3. El usuario confirma el registro.

Una medición fuera de rango no convierte la toma en incompleta.

Ejemplo para un ambiente con tres mediciones requeridas:

```text
Registro de UMA 1 - horario 07:00
├── Temperatura ambiental: registrada
├── Humedad relativa: registrada
└── Temperatura de refrigeración: registrada

Resultado: registro completo
```

Si falta cualquiera de los tres valores, el horario todavía no se cuenta como completado.

## 9. Porcentaje de avance

El avance diario se calcula por rondas completas, no por cantidad de valores individuales.

```text
Avance diario = registros completos / registros esperados × 100
```

Ejemplo de UMA 3:

- Registros esperados: 3.
- Registros completos: 2.
- Avance diario: `2 / 3 × 100 = 66.67 %`.

Ejemplo de UMA 1:

- Registros esperados: 4.
- Registros completos: 4.
- Avance diario: `4 / 4 × 100 = 100 %`.

El cálculo usa la configuración que estaba vigente para el ambiente en la fecha operativa consultada.

### Indicadores del panel

Se recomienda mostrar dos indicadores:

1. **Avance diario:** completados frente a todos los horarios esperados del día operativo.
2. **Cumplimiento al momento:** completados frente a los horarios cuya ventana ya comenzó o terminó.

Esto evita que un ambiente parezca incumplido a las 08:00 por no haber realizado todavía los registros de las 12:00, 19:00 y 00:00.

Los registros tardíos cuentan para el avance diario, pero deben poder distinguirse en reportes futuros.

## 10. Flujo principal del registrador

1. El usuario inicia sesión.
2. El sistema selecciona su ambiente predeterminado.
3. El usuario puede cambiar a otro ambiente autorizado.
4. El sistema identifica el día operativo y el horario correspondiente.
5. La pantalla muestra únicamente las mediciones requeridas por el ambiente.
6. El usuario ingresa todos los valores.
7. El sistema indica visualmente cuáles están fuera de rango.
8. El usuario confirma el registro.
9. El sistema guarda la toma, sus detalles y actualiza el porcentaje de avance.

## 11. Flujo principal del supervisor

1. El supervisor inicia sesión.
2. Visualiza todos los ambientes.
3. Consulta el avance diario de cada ambiente.
4. Identifica registros pendientes, completos, tardíos o con valores fuera de rango.
5. Consulta el detalle de un registro.

Las acciones administrativas y permisos específicos se definirán en una fase posterior.

## 12. Reglas de integridad

- No se puede confirmar dos veces el mismo ambiente, día operativo y horario.
- Un usuario solo puede registrar en ambientes asignados.
- El ambiente predeterminado debe pertenecer a las asignaciones activas del usuario.
- Un registro debe contener exactamente las mediciones requeridas por la configuración vigente.
- El rango mínimo no puede ser mayor que el rango máximo.
- Los registros confirmados no se modifican ni eliminan en el MVP.
- La fecha y hora real deben generarse en el servidor y no ser proporcionadas libremente por el cliente.
- Todas las fechas y horas deben interpretarse usando la zona horaria configurada para el establecimiento.

## 13. Arquitectura inicial propuesta

La aplicación será una web monolítica con ASP.NET Core. Las pantallas, la lógica de negocio y el acceso a datos formarán parte de una sola aplicación desplegable; no se crearán un backend API y un frontend independiente.

Para este alcance se propone usar Razor Pages, adecuado para un sistema pequeño centrado en inicio de sesión, formularios de captura y consultas.

Estructura inicial sugerida:

```text
Temperatura.sln
└── Temperatura.Web
    ├── Data
    ├── Domain
    ├── Pages
    ├── Services
    └── wwwroot
```

Responsabilidades:

- `Data`: Entity Framework Core, contexto, configuraciones y migraciones.
- `Domain`: entidades y reglas esenciales del negocio.
- `Pages`: pantallas Razor Pages y sus modelos.
- `Services`: casos de uso y cálculos, como la fecha operativa y el avance.
- `wwwroot`: estilos, JavaScript e imágenes de la interfaz.

Las pruebas automatizadas se mantienen en el proyecto `Temperatura.Tests`; esto no divide la aplicación en backend y frontend.

Decisiones técnicas:

- Base de datos: SQL Server.
- Entorno inicial de base de datos: instancia existente ejecutándose en Docker.
- Acceso a datos propuesto: Entity Framework Core con migraciones.
- Tipo de aplicación: web monolítica con ASP.NET Core Razor Pages.
- Autenticación propuesta: ASP.NET Core Identity con usuarios almacenados en SQL Server.
- Versión inicial: .NET 10.
- Interfaz administrativa: Tabler 1.4 con recursos locales administrados mediante npm.

Decisiones técnicas todavía pendientes:

- Lugar de despliegue.

## 14. Alcance sugerido del MVP

1. Inicio y cierre de sesión.
2. Selección entre los ambientes asignados al usuario.
3. Captura manual y confirmación de registros dentro de la ventana permitida.
4. Detección visual de valores fuera de rango.
5. Identificación de registros puntuales y tardíos.
6. Visualización del avance diario del ambiente.
7. Consulta del historial sin edición ni eliminación.

Los ambientes, horarios, mediciones, rangos y asignaciones iniciales se cargan como datos iniciales. El supervisor ya puede administrar usuarios, asignarles ambientes, crear ambientes y configurar sus mediciones y rangos. La configuración de horarios y la vista completa de supervisión se incorporarán en las siguientes etapas.

## 15. Decisiones pendientes antes de programar

- Confirmar la versión de .NET que se utilizará.
- Definir los permisos detallados de cada rol.
- Definir el lugar de despliegue de la aplicación.
- Obtener los datos de conexión de desarrollo para la instancia de SQL Server.

No es necesario definir ahora los rangos reales. El sistema comenzará con datos demostrativos y permitirá que el responsable los configure posteriormente.
