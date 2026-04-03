namespace STHEnterprise.Api.Helpers
{
    public interface IJwtTokenGenerator
    {
        string GenerateToken(AppUser user);
    }
}
