using BepInEx.Configuration;
using RoR2;
using System;
using System.Linq;
using UnityEngine;

namespace RunWatcher
{
    internal class Utils
    {
        public static GameData GetGameData()
        {
            GameData gameData = new();

            ForEachItemIndex((itemIndex) =>
            {
                ItemDef itemDef = ItemCatalog.GetItemDef(itemIndex);
                string itemTier = itemDef.tier.ToString();
                string item = $"Itm-{itemDef.name}-{itemIndex}";

                switch (itemTier)
                {
                    case "Tier1": gameData.Items.Common.Add(item); break;
                    case "Tier2": gameData.Items.Uncommon.Add(item); break;
                    case "Tier3": gameData.Items.Legendary.Add(item); break;
                    case "Boss": gameData.Items.Boss.Add(item); break;
                    case "Lunar": gameData.Items.Lunar.Add(item); break;
                    case "VoidTier1": gameData.Items.Void.Add(item); break;
                    case "VoidTier2": gameData.Items.Void.Add(item); break;
                    case "VoidTier3": gameData.Items.Void.Add(item); break;
                };
            });

            ForEachEquipmentIndex((equipmentIndex) =>
            {
                EquipmentDef equipmentDef = EquipmentCatalog.GetEquipmentDef(equipmentIndex);
                string equipment = $"Eq-{equipmentDef.name}-{equipmentIndex}";
                gameData.Equipment.Add(equipment);
            });

            ForEachSurvivorIndex((survivorIndex) => 
            {
                BodyIndex bodyIndex = SurvivorCatalog.GetBodyIndexFromSurvivorIndex(survivorIndex);
                CharacterBody characterBody = BodyCatalog.GetBodyPrefabBodyComponent(bodyIndex);
                string survivor = $"Srv-{characterBody.baseNameToken.Split('_')[0]}-{survivorIndex}";
                gameData.Survivors.Add(survivor);
            });

            return gameData;
        }

        public static void ForEachItemIndex(Action<ItemIndex> action)
        {
            for (ItemIndex itemIndex = 0; itemIndex < (ItemIndex)ItemCatalog.itemCount; itemIndex++)
            {
               if (!unallowedItemIndex.Contains(itemIndex)) action(itemIndex);
            }
        }

        public static void ForEachEquipmentIndex(Action<EquipmentIndex> action)
        {
            for (EquipmentIndex equipmentIndex = 0; equipmentIndex < (EquipmentIndex)EquipmentCatalog.equipmentCount; equipmentIndex++)
            {
                if (!unallowedEquipmentIndex.Contains(equipmentIndex)) action(equipmentIndex);
            }
        }

        public static void ForEachSurvivorIndex(Action<SurvivorIndex> action)
        {
            for (SurvivorIndex survivorIndex = 0; survivorIndex < (SurvivorIndex)SurvivorCatalog.survivorCount; survivorIndex++)
            {
                action(survivorIndex);
            }
        }

        public static Texture2D RerenderTexture2D(Texture texture)
        {
            RenderTexture tmp = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Graphics.Blit(texture, tmp);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = tmp;
            Texture2D itemBGTex = new(texture.width, texture.height, TextureFormat.ARGB32, false);
            itemBGTex.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
            itemBGTex.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(tmp);

            return itemBGTex; 
        }

        private static readonly ItemIndex[] unallowedItemIndex =
        [
            (ItemIndex)0,
            (ItemIndex)20,
            (ItemIndex)21,
            (ItemIndex)23,
            (ItemIndex)32,
            (ItemIndex)33,
            (ItemIndex)40,
            (ItemIndex)43,
            (ItemIndex)45,
            (ItemIndex)46,
            (ItemIndex)47,
            (ItemIndex)70,
            (ItemIndex)74,
            (ItemIndex)82,
            (ItemIndex)91,
            (ItemIndex)97,
            (ItemIndex)108,
            (ItemIndex)110,
            (ItemIndex)111,
            (ItemIndex)115,
            (ItemIndex)131,
            (ItemIndex)132,
            (ItemIndex)168,
            (ItemIndex)169,
            (ItemIndex)177,
            (ItemIndex)180,
        ];

        private static readonly EquipmentIndex[] unallowedEquipmentIndex =
        [
            (EquipmentIndex)13,
            (EquipmentIndex)19,
            (EquipmentIndex)21,
            (EquipmentIndex)22,
            (EquipmentIndex)27,
            (EquipmentIndex)30,
            (EquipmentIndex)34,
            (EquipmentIndex)35,
            (EquipmentIndex)40,
            (EquipmentIndex)39,
            (EquipmentIndex)46,
            (EquipmentIndex)47,
        ];
    }
}
