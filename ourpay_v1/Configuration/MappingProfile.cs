using AutoMapper;
using PaymentApi.DTOs;
using PaymentApi.Models;

namespace PaymentApi.Configuration;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // User mappings
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()));

        // Payment mappings
        CreateMap<Payment, PaymentDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.MerchantName, opt => opt.MapFrom(src => src.Merchant.Name))
            .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.Customer != null ? src.Customer.Name : null));

        CreateMap<CreatePaymentRequest, Payment>()
            .ForMember(dest => dest.PaymentId, opt => opt.Ignore())
            .ForMember(dest => dest.MerchantId, opt => opt.Ignore())
            .ForMember(dest => dest.CustomerId, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            .ForMember(dest => dest.ExpiresAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Merchant, opt => opt.Ignore())
            .ForMember(dest => dest.Customer, opt => opt.Ignore())
            .ForMember(dest => dest.AuditLogs, opt => opt.Ignore());

        CreateMap<Payment, CreatePaymentResponse>()
            .ForMember(dest => dest.PaymentLink, opt => opt.Ignore());

        CreateMap<Payment, ConfirmPaymentResponse>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.ProcessedAt, opt => opt.MapFrom(src => src.UpdatedAt));

        // Webhook mappings
        CreateMap<Webhook, WebhookDto>()
            .ForMember(dest => dest.EventTypes, opt => opt.MapFrom(src => src.EventTypes));

        CreateMap<CreateWebhookRequest, Webhook>()
            .ForMember(dest => dest.WebhookId, opt => opt.Ignore())
            .ForMember(dest => dest.MerchantId, opt => opt.Ignore())
            .ForMember(dest => dest.EventTypes, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Merchant, opt => opt.Ignore());

        CreateMap<WebhookEvent, WebhookEventDto>()
            .ForMember(dest => dest.Data, opt => opt.MapFrom(src => DeserializeEventData(src.EventData)));

        CreateMap<Models.WebhookDeliveryAttempt, DTOs.WebhookDeliveryAttempt>();
    }

    private object DeserializeEventData(string eventData)
    {
        return System.Text.Json.JsonSerializer.Deserialize<object>(eventData) ?? new object();
    }
}
