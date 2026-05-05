namespace TraceabilitySystem.Shared.Constants;

public static class AppConstants
{
    public const string DefaultAdminEmail = "admin@TraceabilitySystem.com";
    public const string DefaultAdminPassword = "Admin@123456";
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 100;

    public static class Roles
    {
        public const string Admin = "Admin";
        public const string User = "User";
    }

    public static class Jwt
    {
        public const int AccessTokenExpirationMinutes = 60;
        public const int RefreshTokenExpirationDays = 7;
    }
}
