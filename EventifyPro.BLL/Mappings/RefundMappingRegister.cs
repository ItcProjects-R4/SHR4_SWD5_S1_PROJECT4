namespace EventifyPro.BLL.Mappings;

#pragma warning disable CS8603 // Mapster Ignore expressions can target nullable members safely.

public sealed class RefundMappingRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<RefundCreateDto, Refund>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.BookingId)
            .Ignore(dest => dest.TransactionId)
            .Ignore(dest => dest.InitiatedById)
            .Ignore(dest => dest.ProcessedAt)
            .Ignore(dest => dest.Payment)
            .Ignore(dest => dest.Booking)
            .Ignore(dest => dest.Initiator)
            .Map(dest => dest.Status, _ => RefundStatus.Pending)
            .Map(dest => dest.CreatedAt, _ => DateTime.UtcNow);

        config.NewConfig<Refund, RefundResponseDto>()
            .Map(dest => dest.Status, src => src.Status.ToString())
            .Map(dest => dest.ProcessedByAdminId, src => src.InitiatedById)
            .Map(dest => dest.UpdatedAt, src => src.ProcessedAt);
    }
}

#pragma warning restore CS8603
