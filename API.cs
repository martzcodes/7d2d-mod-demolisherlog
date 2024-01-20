using HarmonyLib;
using System.Collections.Generic;
using System;
using System.Reflection;

public class API : IModApi
{
    private string ServerChatName = "Server";

    public void InitMod(Mod _modInstance)
    {
        Harmony harmony = new Harmony(GetType().ToString());
        harmony.PatchAll(Assembly.GetExecutingAssembly());

        //This registers a handler for when the server handles a chat message.
        ModEvents.ChatMessage.RegisterHandler(ChatMessage);
    }

    [HarmonyPatch(typeof(EntityZombieCop))]
    public class Patches_EntityZombieCop
    {

        [HarmonyPostfix]
        [HarmonyPatch("ProcessDamageResponseLocal")]
        public static void ProcessDamageResponseLocal(EntityZombieCop __instance, DamageResponse _dmResponse)
        {

            if (!__instance.isEntityRemote && (_dmResponse.HitBodyPart & EnumBodyPartHit.Special) > EnumBodyPartHit.None)
            {
                EntityAlive entityAlive = __instance.world.GetEntity(_dmResponse.Source.getEntityId()) as EntityAlive;
                if (entityAlive && entityAlive is EntityPlayer ep)
                {
                    Log.Out("Player: {0} {1} primed detonator {2}", ep.EntityName, ep.entityId, __instance.entityId);
                    ChatHook.ChatMessage(null, $"{ep.EntityName} primed detonator", -1, "Server", EChatType.Global, null);
                }
            }
        }
    }

    [HarmonyPatch(typeof(BlockMine))]
    public class Patches_BlockMine
    {

        [HarmonyPostfix]
        [HarmonyPatch("OnEntityWalking")]
        public static void OnEntityWalking(BlockMine __instance, WorldBase _world, int _x, int _y, int _z, BlockValue _blockValue, Entity entity)
        {
            if (EffectManager.GetValue(PassiveEffects.LandMineImmunity, null, 0f, entity as EntityAlive) != 0f)
            {
                return;
            }
            if (entity as EntityPlayer != null)
            {
                if ((entity as EntityPlayer).IsSpectator)
                {
                    return;
                }
                EntityPlayer entityPlayer = _world.GetEntity(entity.entityId) as EntityPlayer;
                Log.Out("Player: {0} {1} stepped on a mine {2}", entityPlayer.EntityName, entityPlayer.entityId, __instance.blockID);
                ChatHook.ChatMessage(null, $"{entityPlayer.EntityName} stepped on a mine", -1, "Server", EChatType.Global, null);
            }
        }
    }

    //This method will then be called every time a ChatMessage is sent.
    private bool ChatMessage(ClientInfo clientInfo, EChatType _type, int _senderId, string message, string mainName, bool _localizeMain, List<int> _recipientEntityIds)
    {
        //We make sure there is an actual message and a client, and also ignore the message if it's from the server.
        if (!string.IsNullOrEmpty(message) && clientInfo != null && mainName != ServerChatName)
        {
            //We check to see if the message starts with a /
            if (message.StartsWith("/"))
            {
                //we then remove that / to get the rest of the message.
                message = message.Replace("/", "");

                if (message == "hello")
                {
                    ChatHook.ChatMessage(clientInfo, $"Hello {clientInfo.playerName}", -1, "Server", EChatType.Global, null);
                    //We return false to prevent any other listeners from processing this message.
                    return false;
                }
            }
        }
        //Returning true allows other listeners to process this message.
        return true;
    }

    public class ChatHook
    {
        public static void ChatMessage(ClientInfo _cInfo, string _message, int _senderId, string _name, EChatType _type, List<int> _recipientEntityIds)
        {
            try
            {
                if (string.IsNullOrEmpty(_message) || _message.Contains("U+") || _name.Contains("U+"))
                {
                    return;
                }
                GameManager.Instance.ChatMessageServer(_cInfo, _type, -1, _message, _name, false, _recipientEntityIds);
            }
            catch (Exception e)
            {
                Log.Out(string.Format("Error in ChatHook.ChatMessage: {0}", e.Message));
            }
        }
    }
}
