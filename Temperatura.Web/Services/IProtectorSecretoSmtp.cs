namespace Temperatura.Web.Services;

public interface IProtectorSecretoSmtp
{
    string Proteger(string secreto);

    string Desproteger(string secretoProtegido);
}
