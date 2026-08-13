using Microsoft.AspNetCore.DataProtection;

namespace Temperatura.Web.Services;

public sealed class ProtectorSecretoSmtp : IProtectorSecretoSmtp
{
    private readonly IDataProtector _protector;

    public ProtectorSecretoSmtp(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector(
            "Temperatura.Web.ConfiguracionSmtp.Secreto.v1");
    }

    public string Proteger(string secreto) => _protector.Protect(secreto);

    public string Desproteger(string secretoProtegido) => _protector.Unprotect(secretoProtegido);
}
