using UnityEngine;
using PurrNet;
using System.Collections.Generic;

public class StationManager : NetworkBehaviour
{
    [Header("Room/Round number")]
    public int roundNumber = 1;

    [Header("Stations")]
    public AnswerButtonManager[] stations;

    public void AssignStations(List<IPlayerController> activePlayers)
    {
        if (!isServer)
        {
            return;
        }

        for (int i = 0; i < stations.Length; i++)
        {
            if (stations[i] == null)
            {

                continue;
            }

            if (i < activePlayers.Count)
            {
                stations[i].RPC_SetOwner(activePlayers[i].gameObject.GetComponent<NetworkIdentity>());
                RPC_SetStationActive(i, true);
            }
            else
            {
                stations[i].RPC_SetOwner(null);
                RPC_SetStationActive(i, false);
            }
        }
    }

    [ObserversRpc]
    private void RPC_SetStationActive(int stationIndex, bool active)
    {
        if (stationIndex >= 0 && stationIndex < stations.Length && stations[stationIndex] != null)
            stations[stationIndex].gameObject.SetActive(active);
    }

}
