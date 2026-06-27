namespace EventifyPro.Web.ViewModels.Account
{
    public class ForgotPasswordViewModel
    {

        [Required(ErrorMessage = "Please enter your email address.")]
        [EmailAddress(ErrorMessage = "Invalid email address format.")]
        public string Email { get; set; } = string.Empty;

    }
}
