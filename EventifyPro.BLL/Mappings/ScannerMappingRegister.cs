using EventifyPro.BLL.DTOs.Scanner;
using Mapster;

namespace EventifyPro.BLL.Mappings;

public sealed class ScannerMappingRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<ScanLog, ScanLogResponseDto>()
            .Map(dest => dest.ScannerName, src => src.Scanner.FullName)
            .Map(dest => dest.ScanResult, src => src.Result.ToString())
            .Map(dest => dest.RawQRData, src => src.RawQRCode);
    }
}
