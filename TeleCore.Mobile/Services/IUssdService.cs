namespace TeleCore.Mobile.Services
{
    public interface IUssdService
    {
        void DialUssd(string code, int simId); // إضافة simId هنا
    }
}