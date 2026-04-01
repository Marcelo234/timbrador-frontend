using AuthBackend.Domain.Interfaces;

namespace AuthBackend.Application.UseCases;

public class CerrarSesionUseCase
{
    private readonly ISesionRepository _sesionRepository;

    public CerrarSesionUseCase(ISesionRepository sesionRepository)
    {
        _sesionRepository = sesionRepository;
    }

    public async Task Ejecutar(string refreshToken)
    {
        await _sesionRepository.FinalizarSesion(refreshToken);
    }
}
