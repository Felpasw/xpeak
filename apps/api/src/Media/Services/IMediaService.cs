using Xpeak.Api.Media.Dto;

namespace Xpeak.Api.Media.Services;

public interface IMediaService
{
    Task<PresignMediaResponse> RequestPresignsAsync(
        Guid userId,
        Guid checkInId,
        PresignMediaRequest request,
        CancellationToken ct);

    Task<ConfirmMediaResponse> ConfirmMediaAsync(
        Guid userId,
        Guid checkInId,
        ConfirmMediaRequest request,
        CancellationToken ct);
}
