namespace ModernAPIDoc_Scalar.Repos
{
    public interface ITokenService
    {
        string GenerateJwtToken(string username);
    }
}
