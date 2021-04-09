using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MyNetworkManager : NetworkManager
{
    static readonly ILogger logger = LogFactory.GetLogger<NetworkManager>();
    
    public void ChangeSceneForPlayers(NetworkIdentity[] players, string newSceneName)
    {
        if (string.IsNullOrEmpty(newSceneName))
        {
            logger.LogError("ServerChangeScene empty scene name");
            return;
        }

        if (logger.logEnabled) logger.Log("ServerChangeScene " + newSceneName);
        foreach (var player in players)
        {
            NetworkServer.SetClientNotReady(player.connectionToClient);
        }
        
        if (isHeadless) loadingSceneAsync = SceneManager.LoadSceneAsync(newSceneName, LoadSceneMode.Additive);
        else loadingSceneAsync = SceneManager.LoadSceneAsync(newSceneName);
        foreach (var player in players)
        {
            NetworkServer.SendToClientOfPlayer(player, new SceneMessage{sceneName = newSceneName});    
        }
    }
}