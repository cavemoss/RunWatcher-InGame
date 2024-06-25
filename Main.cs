using BepInEx;
using RoR2;
using UnityEngine;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BepInEx.Configuration;


namespace RunWatcher
{
    [BepInPlugin("cavemoss.RunWatcher", "RunWatcher", "1.0.0")]
    public class Main : BaseUnityPlugin
    {
        public void Awake()
        {
            string vueAppPath = $@"{System.IO.Path.GetDirectoryName(Info.Location)}\vue-app";

            ResolveIp(vueAppPath);
            On.RoR2.Run.Start += Run_Start;
            On.RoR2.Run.OnDestroy += delegate(On.RoR2.Run.orig_OnDestroy orig, Run self)
            {
                orig.Invoke(self);
                runStarted = false;
                foreach (Server webHook in ServerInstance)
                {
                    webHook.SendUpdate(RunData.GetCurrentRunData());
                }
            };  
        }

        private void ResolveIp(string vueAppPath)
        {
            string urlPath = $@"{vueAppPath}\public\server-url-list.txt";
            string urlListTxt = "";

            ServerPort = Config.Bind("Config", "Server Port", 8080);
            int port = ServerPort.Value - 1;

            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus == OperationalStatus.Up && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                {
                    UnicastIPAddressInformationCollection ipAll = ni.GetIPProperties().UnicastAddresses;

                    for (int i = 0; i < ipAll.Count; i++)
                    {
                        UnicastIPAddressInformation ip = ipAll[i];
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            port++;
                            string serverUrl = $"http://{ip.Address}:{port}";

                            urlListTxt += serverUrl + "\n"; 
                            Server server = new(serverUrl);

                            server.Start();
                            ServerInstance.Add(server);
                        }
                    }
                }
            }

            File.WriteAllText(urlPath, urlListTxt.Substring(0, urlListTxt.Length - 1));
        }

        private void InitIconsDir()
        {
            string vueAppPath = $@"{System.IO.Path.GetDirectoryName(Info.Location)}\vue-app";
            string iconsPath = $@"{vueAppPath}\public\icons";

            if (Directory.Exists(iconsPath)) { return; }
            else
            {
                Directory.CreateDirectory(iconsPath);

                Utils.ForEachItemIndex((itemIndex) =>
                {
                    ItemDef itemDef = ItemCatalog.GetItemDef(itemIndex);
                    if (itemDef.pickupIconSprite != null)
                    {
                        Texture2D itemBGTex = Utils.RerenderTexture2D(itemDef.pickupIconSprite.texture);
                        byte[] itemBGBytes = itemBGTex.EncodeToPNG();

                        string filePath = $@"{iconsPath}\Itm-{itemDef.name}-{itemIndex}.png";
                        if (!Directory.Exists(filePath)) { File.WriteAllBytes(filePath, itemBGBytes); }
                    }
                });

                Utils.ForEachEquipmentIndex((equipmentIndex) =>
                {
                    EquipmentDef equipmentDef = EquipmentCatalog.GetEquipmentDef(equipmentIndex);
                    if (equipmentDef.pickupIconSprite != null)
                    {
                        Texture2D itemBGTex = Utils.RerenderTexture2D(equipmentDef.pickupIconSprite.texture);
                        byte[] itemBGBytes = itemBGTex.EncodeToPNG();

                        string filePath = $@"{iconsPath}\Eq-{equipmentDef.name}-{equipmentIndex}.png";
                        if (!Directory.Exists(filePath)) { File.WriteAllBytes(filePath, itemBGBytes); }
                    }
                });

                Utils.ForEachSurvivorIndex((survivorIndex) =>
                {
                    BodyIndex bodyIndex = SurvivorCatalog.GetBodyIndexFromSurvivorIndex(survivorIndex);
                    CharacterBody characterBody = BodyCatalog.GetBodyPrefabBodyComponent(bodyIndex);

                    if (characterBody.portraitIcon != null)
                    {
                        Texture2D itemBGTex = Utils.RerenderTexture2D(characterBody.portraitIcon);
                        byte[] itemBGBytes = itemBGTex.EncodeToPNG();

                        string filePath = $@"{iconsPath}\Srv-{characterBody.baseNameToken.Split('_')[0]}-{survivorIndex}.png";
                        if (!Directory.Exists(filePath)) { File.WriteAllBytes(filePath, itemBGBytes); }
                    }
                });
            }
        }

        private async void Run_Start(On.RoR2.Run.orig_Start orig, Run self)
        {
            orig.Invoke(self);

            if (!runStarted)
            {
                InitIconsDir();

                CharacterBody characterBody = null;

                foreach (KeyValuePair<NetworkUserId, CharacterMaster> userMaster in self.userMasters)
                {
                    while (!userMaster.Value.GetBody()) { await Task.Delay(500); }

                    characterBody = userMaster.Value.GetBody();

                    break;
                }

                string survivorName = characterBody.baseNameToken.Split('_')[0];

                await Task.Run(async () =>
                {
                    runStarted = true;

                    RunData.SurvivorName = survivorName;

                    while (runStarted)
                    {
                        UpdateInventory(characterBody.inventory);

                        if (RunData.GetCurrentRunData() != prevUpdate)
                        {
                            foreach (Server webHook in ServerInstance)
                            {
                                webHook.SendUpdate(RunData.GetCurrentRunData());
                            }
                        }

                        prevUpdate = RunData.GetCurrentRunData();
                        await Task.Delay(1000);
                    }
                });
            }
        }   

        private void UpdateInventory(Inventory inventory)
        {
            RunData.Items.Clear();

            Utils.ForEachItemIndex((itemIndex) =>
            {
                if (inventory.GetItemCount(itemIndex) > 0)
                {
                    ItemDef itemDef = ItemCatalog.GetItemDef(itemIndex);
                    RunData.Items.Add(new {
                        name = $"Itm-{itemDef.name}-{itemIndex}", 
                        tier = itemDef.tier.ToString(), 
                        count = inventory.GetItemCount(itemIndex) 
                    });
                }
            });

            for (uint slot = 0U; slot < (uint)inventory.GetEquipmentSlotCount(); slot+=1U)
            {
                EquipmentState equipmentState = inventory.GetEquipment(slot);
                EquipmentDef equipmentDef = equipmentState.equipmentDef;
                RunData.Equipment.Add($"{equipmentDef.name}-{equipmentDef.equipmentIndex}");
            }
        }
       
        private bool runStarted = false;

        private string prevUpdate = null;

        public ConfigEntry<int> ServerPort { get; set; }

        private static readonly List<Server> ServerInstance = [];
    }
}