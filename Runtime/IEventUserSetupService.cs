using Xprees.EventLogging.Api.Model;

namespace Xprees.EventLogging
{
    public interface IEventUserSetupService
    {
        User CurrentUser { get; set; }
        void SetFormId(string formId);
        void SetName(string username);
        void SetEmail(string email);
    }
}