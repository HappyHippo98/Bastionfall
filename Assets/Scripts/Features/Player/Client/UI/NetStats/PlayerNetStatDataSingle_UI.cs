using BastionFall.Features.Player.Shared.Authoring.Monitoring;
using TMPro;
using UnityEngine;

namespace BastionFall.Features.Player.Client.UI.NetStats
{
    public class PlayerNetStatDataSingleUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI networkID;
        [SerializeField] private TextMeshProUGUI playerName;
        [SerializeField] private TextMeshProUGUI ping;


        public void Setup(PlayerNetStatsData data, PlayerIdentity identity)
        {
            networkID.text = data.NetworkId.ToString();
            playerName.text = identity.DisplayName.ToString();
            ping.text = data.Fps.ToString();
        }
    }
}