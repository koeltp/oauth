namespace OAuth.Contracts.Auth;

public enum VerificationCodeType
{
    Email,
    Sms
}

public enum VerificationCodePurpose
{
    Login,
    Register,
    ResetPassword,
    BindPhone,
    BindEmail,
    ChangeEmail,
    ChangePhone
}