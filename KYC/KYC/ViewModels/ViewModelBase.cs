namespace KYC.ViewModels
{
    public class ViewModelBase
    {
        public bool IsPostback { get; set; }
        public bool Failed { get; set; }
        public string ErrorMessage { get; set; }
    }
}