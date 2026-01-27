using BepInEx;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GorillaTagCompMod
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class CompScoreboardMod : BaseUnityPlugin
    {
        private Dictionary<string, PlayerStats> playerStats = new Dictionary<string, PlayerStats>();
        private Rect scoreboardRect = new Rect(20, 20, 600, 600);
        private string currentGameMode = "";
        private string localPlayerName = "";
        private Dictionary<string, Color> playerColors = new Dictionary<string, Color>();
        private List<string> recentMessages = new List<string>();
        private List<float> messageTimestamps = new List<float>();

        void Start()
        {
            Debug.Log("===================================");
            Debug.Log("AUTO TEAM TRACKER LOADED!");
            Debug.Log("===================================");
        }

        void Update()
        {
            UpdateGameState();
            CleanupOldMessages();
        }

        void UpdateGameState()
        {
            if (PhotonNetwork.InRoom)
            {
                Room currentRoom = PhotonNetwork.CurrentRoom;

                if (currentRoom.Name.ToLower().Contains("infection"))
                    currentGameMode = "INFECTION";
                else if (currentRoom.Name.ToLower().Contains("hunt"))
                    currentGameMode = "HUNT";
                else if (currentRoom.Name.ToLower().Contains("casual"))
                    currentGameMode = "CASUAL";
                else
                    currentGameMode = "TAG";

                foreach (Photon.Realtime.Player player in PhotonNetwork.PlayerList)
                {
                    if (!playerStats.ContainsKey(player.NickName))
                    {
                        playerStats[player.NickName] = new PlayerStats(player.NickName);

                        Color playerColor = GetPlayerColor(player);
                        playerColors[player.NickName] = playerColor;

                        string colorName = GetColorName(playerColor);
                        AddMessage($"{player.NickName} joined {colorName} Team!");
                        Debug.Log($"★ {player.NickName} joined {colorName} Team");
                    }

                    playerStats[player.NickName].totalPlayTime += Time.deltaTime;
                }

                List<string> playersToRemove = new List<string>();
                foreach (string playerName in playerStats.Keys)
                {
                    bool stillInRoom = false;
                    foreach (Photon.Realtime.Player player in PhotonNetwork.PlayerList)
                    {
                        if (player.NickName == playerName)
                        {
                            stillInRoom = true;
                            break;
                        }
                    }
                    if (!stillInRoom)
                    {
                        playersToRemove.Add(playerName);
                    }
                }

                foreach (string playerName in playersToRemove)
                {
                    playerStats.Remove(playerName);
                    playerColors.Remove(playerName);
                    AddMessage($"{playerName} left the game");
                    Debug.Log($"✗ {playerName} left");
                }

                if (PhotonNetwork.LocalPlayer != null)
                {
                    localPlayerName = PhotonNetwork.LocalPlayer.NickName;
                }
            }
            else
            {
                if (playerStats.Count > 0)
                {
                    playerStats.Clear();
                    playerColors.Clear();
                    recentMessages.Clear();
                    messageTimestamps.Clear();
                }
            }
        }

        Color GetPlayerColor(Photon.Realtime.Player player)
        {
            VRRig[] rigs = FindObjectsOfType<VRRig>();
            foreach (VRRig rig in rigs)
            {
                PhotonView view = rig.GetComponent<PhotonView>();
                if (view != null && view.Owner != null)
                {
                    if (view.Owner.UserId == player.UserId)
                    {
                        if (rig.materialsToChangeTo != null && rig.materialsToChangeTo.Length > 0)
                        {
                            return rig.materialsToChangeTo[0].color;
                        }
                    }
                }
            }
            return Color.white;
        }

        string GetColorName(Color color)
        {
            if (IsColorClose(color, Color.red))
                return "Red";
            else if (IsColorClose(color, Color.blue))
                return "Blue";
            else if (IsColorClose(color, Color.green))
                return "Green";
            else if (IsColorClose(color, new Color(1f, 0.5f, 0f)))
                return "Orange";
            else if (IsColorClose(color, new Color(1f, 0.4f, 0.7f)))
                return "Pink";
            else if (IsColorClose(color, new Color(0.5f, 0f, 0.5f)))
                return "Purple";
            else if (IsColorClose(color, Color.yellow))
                return "Yellow";
            else if (IsColorClose(color, Color.black))
                return "Black";
            else if (IsColorClose(color, Color.white))
                return "White";
            else
                return "Other";
        }

        bool IsColorClose(Color a, Color b)
        {
            float threshold = 0.3f;
            return Vector3.Distance(new Vector3(a.r, a.g, a.b), new Vector3(b.r, b.g, b.b)) < threshold;
        }

        void AddMessage(string message)
        {
            recentMessages.Add(message);
            messageTimestamps.Add(Time.time);
        }

        void CleanupOldMessages()
        {
            for (int i = recentMessages.Count - 1; i >= 0; i--)
            {
                if (Time.time - messageTimestamps[i] > 5f)
                {
                    recentMessages.RemoveAt(i);
                    messageTimestamps.RemoveAt(i);
                }
            }
        }

        void OnGUI()
        {
            GUI.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);
            GUI.Box(scoreboardRect, "");

            GUILayout.BeginArea(scoreboardRect);

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("<b><size=22>TEAM TRACKER</size></b>", GetHeaderStyle());
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            if (PhotonNetwork.InRoom)
            {
                GUILayout.Label($"<b>Room:</b> {PhotonNetwork.CurrentRoom.Name}", GetLabelStyle());
                GUILayout.Label($"<b>Mode:</b> {currentGameMode}", GetLabelStyle());
                GUILayout.Label($"<b>Total Players:</b> {PhotonNetwork.PlayerList.Length}", GetLabelStyle());
            }
            else
            {
                GUILayout.Label("<color=yellow>NOT IN A ROOM</color>", GetLabelStyle());
            }

            GUILayout.Space(15);

            GUILayout.Label("<b>═══ TEAMS ═══</b>", GetHeaderStyle());
            GUILayout.Space(10);

            var teamGroups = playerColors.GroupBy(x => GetColorName(x.Value)).OrderBy(g => g.Key);

            foreach (var teamGroup in teamGroups)
            {
                Color teamColor = GetTeamDisplayColor(teamGroup.Key);
                GUIStyle teamStyle = GetTeamStyle(teamColor);

                GUILayout.Label($"{teamGroup.Key} Team ({teamGroup.Count()} players):", teamStyle);
                GUILayout.Space(5);

                foreach (var player in teamGroup)
                {
                    bool isLocal = player.Key == localPlayerName;
                    GUIStyle nameStyle = isLocal ? GetLocalPlayerStyle() : GetPlayerStyle();
                    GUILayout.Label($"  • {player.Key}", nameStyle);
                }

                GUILayout.Space(10);
            }

            if (playerStats.Count == 0)
            {
                GUILayout.Space(20);
                GUILayout.Label("<color=yellow>Waiting for players...</color>", GetLabelStyle());
            }

            GUILayout.Space(15);

            if (recentMessages.Count > 0)
            {
                GUILayout.Label("<b>═══ RECENT ACTIVITY ═══</b>", GetHeaderStyle());
                GUILayout.Space(5);
                int displayCount = Mathf.Min(5, recentMessages.Count);
                for (int i = recentMessages.Count - 1; i >= recentMessages.Count - displayCount; i--)
                {
                    GUILayout.Label($"• {recentMessages[i]}", GetSmallLabelStyle());
                }
            }

            GUILayout.EndArea();
        }

        Color GetTeamDisplayColor(string teamName)
        {
            switch (teamName)
            {
                case "Red": return Color.red;
                case "Blue": return Color.blue;
                case "Green": return Color.green;
                case "Orange": return new Color(1f, 0.5f, 0f);
                case "Pink": return new Color(1f, 0.4f, 0.7f);
                case "Purple": return new Color(0.5f, 0f, 0.5f);
                case "Yellow": return Color.yellow;
                case "Black": return Color.black;
                case "White": return Color.white;
                default: return Color.gray;
            }
        }

        GUIStyle GetHeaderStyle()
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.alignment = TextAnchor.MiddleCenter;
            style.normal.textColor = new Color(0.4f, 0.9f, 1f);
            style.richText = true;
            style.fontStyle = FontStyle.Bold;
            style.fontSize = 14;
            return style;
        }

        GUIStyle GetLabelStyle()
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.normal.textColor = Color.white;
            style.richText = true;
            style.fontSize = 13;
            return style;
        }

        GUIStyle GetSmallLabelStyle()
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.normal.textColor = new Color(0.85f, 0.85f, 0.85f);
            style.fontSize = 11;
            style.richText = true;
            return style;
        }

        GUIStyle GetPlayerStyle()
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.normal.textColor = Color.white;
            style.fontSize = 12;
            return style;
        }

        GUIStyle GetLocalPlayerStyle()
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.normal.textColor = new Color(0.3f, 1f, 0.3f);
            style.fontStyle = FontStyle.Bold;
            style.fontSize = 12;
            return style;
        }

        GUIStyle GetTeamStyle(Color color)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.normal.textColor = color;
            style.fontStyle = FontStyle.Bold;
            style.fontSize = 13;
            return style;
        }
    }

    public class PlayerStats
    {
        public string playerName;
        public float totalPlayTime = 0f;

        public PlayerStats(string name)
        {
            playerName = name;
        }
    }

    public static class PluginInfo
    {
        public const string GUID = "com.competitive.gorillatag.autoteamtracker";
        public const string Name = "AutoTeamTracker";
        public const string Version = "4.0.0";
    }
}