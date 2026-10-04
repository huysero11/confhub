namespace ConfHub.Application.Accounts.VerifyEmail;

// Status: "Active" hoặc "PendingApproval" → FE chọn thông báo phù hợp.
public sealed record VerifyEmailResponse(string Status);
