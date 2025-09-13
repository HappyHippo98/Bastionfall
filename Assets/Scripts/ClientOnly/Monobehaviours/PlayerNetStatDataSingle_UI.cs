using Shared.Authoring.Monitoring;
using TMPro;
using UnityEngine;

namespace ClientOnly.Monobehaviours
{
    public class PlayerNetStatDataSingleUI : MonoBehaviour
    {

        [SerializeField] private TextMeshProUGUI networkID;
        [SerializeField] private TextMeshProUGUI playerName;
        [SerializeField] private TextMeshProUGUI ping;


        private void Awake()
        {
            
        }
        
        private void Update()
        {
            
        }

        public void Setup(PlayerNetStatsData data)
        {
            networkID.text = data.NetworkId.ToString();
            playerName.text = $"{Mathf.RoundToInt(data.RttMs)} ms";
            ping.text = data.Fps.ToString();
        }

        
        
        
    }
}