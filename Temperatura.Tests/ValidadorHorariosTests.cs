using Temperatura.Web.Services;

namespace Temperatura.Tests;

public class ValidadorHorariosTests
{
    // Catálogo vigente: 07:00 / 13:00 / 19:00 y la ronda de madrugada de 01:00.
    private static readonly HorarioValidable[] Catalogo =
    [
        new(1, new TimeOnly(7, 0), false, true),
        new(2, new TimeOnly(13, 0), false, true),
        new(3, new TimeOnly(19, 0), false, true),
        new(4, new TimeOnly(1, 0), true, true)
    ];

    [Fact]
    public void AceptaUnCambioDeHoraValido()
    {
        var candidato = new HorarioValidable(2, new TimeOnly(14, 0), false, true);

        Assert.Empty(ValidadorHorarios.Validar(candidato, Catalogo));
    }

    [Fact]
    public void RechazaUnaHoraYaUsadaPorOtroHorario()
    {
        var candidato = new HorarioValidable(2, new TimeOnly(19, 0), false, true);

        var error = Assert.Single(ValidadorHorarios.Validar(candidato, Catalogo));

        Assert.Equal(ValidadorHorarios.ClaveHora, error.Clave);
    }

    [Fact]
    public void PermiteConservarLaPropiaHoraAlEditar()
    {
        var candidato = new HorarioValidable(2, new TimeOnly(13, 0), false, true);

        Assert.Empty(ValidadorHorarios.Validar(candidato, Catalogo));
    }

    [Fact]
    public void RechazaLaHoraDeUnHorarioInactivoPorqueElIndiceEsUnico()
    {
        var catalogo = Catalogo.Append(new HorarioValidable(5, new TimeOnly(22, 0), false, false));
        var candidato = new HorarioValidable(2, new TimeOnly(22, 0), false, true);

        var error = Assert.Single(ValidadorHorarios.Validar(candidato, catalogo));

        Assert.Equal(ValidadorHorarios.ClaveHora, error.Clave);
    }

    [Fact]
    public void RechazaUnCierreDeDiaAnteriorPosteriorALaPrimeraRonda()
    {
        // 09:00 con la bandera de cierre solaparía el día operativo con el siguiente.
        var candidato = new HorarioValidable(4, new TimeOnly(9, 0), true, true);

        var error = Assert.Single(ValidadorHorarios.Validar(candidato, Catalogo));

        Assert.Equal(ValidadorHorarios.ClaveCierre, error.Clave);
    }

    [Fact]
    public void RechazaAdelantarLaPrimeraRondaPorDelanteDelCierreDeMadrugada()
    {
        // Mover la primera ronda a las 00:30 dejaría la de 01:00 (cierre) por detrás.
        var candidato = new HorarioValidable(1, new TimeOnly(0, 30), false, true);

        var error = Assert.Single(ValidadorHorarios.Validar(candidato, Catalogo));

        Assert.Equal(ValidadorHorarios.ClaveCierre, error.Clave);
    }

    [Fact]
    public void RechazaDesactivarLaUltimaRondaDelDia()
    {
        var catalogo = new HorarioValidable[]
        {
            new(1, new TimeOnly(7, 0), false, true),
            new(4, new TimeOnly(1, 0), true, true)
        };
        var candidato = new HorarioValidable(1, new TimeOnly(7, 0), false, Activo: false);

        var error = Assert.Single(ValidadorHorarios.Validar(candidato, catalogo));

        Assert.Equal(ValidadorHorarios.ClaveActivo, error.Clave);
    }

    [Fact]
    public void IgnoraLosHorariosInactivosAlValidarElSolapamiento()
    {
        var catalogo = new HorarioValidable[]
        {
            new(1, new TimeOnly(7, 0), false, true),
            new(5, new TimeOnly(23, 0), true, false)
        };
        var candidato = new HorarioValidable(5, new TimeOnly(23, 0), true, Activo: false);

        Assert.Empty(ValidadorHorarios.Validar(candidato, catalogo));
    }

    [Fact]
    public void GeneraElNombrePredeterminadoDesdeLaHora()
    {
        Assert.Equal("07:00", ValidadorHorarios.NombrePredeterminado(new TimeOnly(7, 0)));
        Assert.Equal("01:00", ValidadorHorarios.NombrePredeterminado(new TimeOnly(1, 0)));
    }
}
