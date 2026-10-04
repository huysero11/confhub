namespace ConfHub.Domain.Accounts;

// Vòng đời tài khoản: docs/diagrams/g1-trang-thai-tai-khoan.puml
public enum UserStatus
{
    Unverified,
    PendingApproval,
    Active,
    Locked
}
