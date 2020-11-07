using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;
using System.Linq;

namespace BladesOfBellevue
{
    public class BotSpawner : NetworkBehaviour
    {
        private NetworkManagerCustom networkManager;

        private float nextNobleSpawn;
        public float minNobelSpawn = 20.0f;
        public float maxNobelSpawn = 40.0f;

        private void Start()
        {
            // next nobel spawn
            nextNobleSpawn = Time.time + UnityEngine.Random.Range(minNobelSpawn, maxNobelSpawn);

            // spawning initial bots
            networkManager = GameObject.FindObjectOfType<NetworkManagerCustom>();

            GameObject botPrefab = networkManager.spawnPrefabs.Find(x => x.name == "ComputerPlayer");
            List<GameObject> nodes = GameObject.FindGameObjectsWithTag("Node").ToList();

            foreach (Player.CitizenType citizenType in Enum.GetValues(typeof(Player.CitizenType)))
            {
                // Don't do special stuff
                if (!(citizenType == Player.CitizenType.beggar || citizenType == Player.CitizenType.noble || citizenType == Player.CitizenType.seer || citizenType == Player.CitizenType.spy))
                {
                    foreach (Player.CitizenColor citizenColor in Enum.GetValues(typeof(Player.CitizenColor)))
                    {
                        GameObject bot = Instantiate(botPrefab);
                        ComputerPlayer computerPlayer = bot.GetComponent<ComputerPlayer>();
                        computerPlayer.citizenType = citizenType;
                        computerPlayer.citizenColor = citizenColor;

                        // just use computer players selection criteria
                        Node spawnNode = computerPlayer.ChooseGoalNode();
                        bot.transform.position = spawnNode.gameObject.transform.position;
                        computerPlayer.currentDistrict = spawnNode.district;
                        computerPlayer.nameText.text = citizenColor.ToString() + citizenType.ToString();

                        NetworkServer.Spawn(bot);
                    }
                }                
            }
        }

        private void Update()
        {
            if (Time.time > nextNobleSpawn)
            {
                GameObject botPrefab = networkManager.spawnPrefabs.Find(x => x.name == "ComputerPlayer");
                List<GameObject> nodes = GameObject.FindGameObjectsWithTag("Node").ToList();

                GameObject bot = Instantiate(botPrefab);
                ComputerPlayer computerPlayer = bot.GetComponent<ComputerPlayer>();
                computerPlayer.citizenType = Player.CitizenType.noble;
                computerPlayer.citizenColor = Player.CitizenColor.blue;

                // just use computer players selection criteria
                Node spawnNode = computerPlayer.ChooseGoalNode();
                bot.transform.position = spawnNode.gameObject.transform.position;
                computerPlayer.currentDistrict = spawnNode.district;
                computerPlayer.nameText.text = computerPlayer.citizenColor.ToString() + computerPlayer.citizenType.ToString();

                NetworkServer.Spawn(bot);

                nextNobleSpawn = Time.time + UnityEngine.Random.Range(minNobelSpawn, maxNobelSpawn);
            }
        }
    }
}

