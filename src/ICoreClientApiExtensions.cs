using System.Reflection;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.Client.NoObf;


namespace ChatTranslator {
    public static class ICoreClientApiExtensions {
        public static void ShowChatMessage(this ICoreClientAPI capi, string message, int groupId, string data = null) {
            var clientEventManager = (ClientEventManager)typeof(ClientMain).GetField("eventManager", 
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                ?.GetValue(capi.World);

            clientEventManager!.TriggerNewServerChatLine(groupId, message, EnumChatType.Notification, data);
        }
        
    }
}
