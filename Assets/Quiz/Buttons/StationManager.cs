using UnityEngine;
using PurrNet;
using System.Collections.Generic;
using GinjaGaming.FinalCharacterController;

public class StationManager : NetworkBehaviour
{
    [Header("Room/Round number")]
    public int roundNumber = 1;

    [Header("Stations")]
    public AnswerButtonManager[] stations;
    
    public void AssignStations(List<MyCharacterController> activePlayers)
    {
        if (!isServer)
        {
            return; 
        }

        for (int i = 0; i < stations.Length; i++)
        {
            if (stations[i] == null)
            {
                Debug.LogWarning($"[StationManager] Station at index {i} is null in round {roundNumber}. Please assign it in the Inspector.");
                continue;
            }
            
            if (i < activePlayers.Count)
            {
                stations[i].RPC_SetOwner(activePlayers[i].GetComponent<NetworkIdentity>());
                stations[i].gameObject.SetActive(true);
            }
            else
            {
                stations[i].RPC_SetOwner(null);
                stations[i].gameObject.SetActive(false);
            }
        }
    }

}
