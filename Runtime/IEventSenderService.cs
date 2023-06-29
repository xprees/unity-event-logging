using Cysharp.Threading.Tasks;
using Xprees.EventLogging.ScriptableObjects;

namespace Xprees.EventLogging
{
    public interface IEventSenderService
    {
        void LogEvent(EventSO loggedEvent);
        UniTask<bool> UploadEvents();
    }
}