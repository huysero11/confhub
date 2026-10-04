namespace ConfHub.Api.IntegrationTests.Support;

// Kind: "VerifyEmail" hoặc "ResetPassword".
public sealed record SentEmail(string Kind, string Email, string Token);
