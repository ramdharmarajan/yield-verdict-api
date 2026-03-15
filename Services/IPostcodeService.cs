using YieldverdictApi.Models.Responses;

namespace YieldverdictApi.Services;

public interface IPostcodeService
{
    Task<PostcodeResult?> ValidateAsync(string postcode);
}
