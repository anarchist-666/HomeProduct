using Oxide.Core.Plugins;
using UnityEngine;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("HomeProduct", "Анархист", "1.0.0")]
    [Description("Удаляет предмет с определенным скином при установке и выполняет вставку через API")]
    public class HomeProduct : RustPlugin
    {
        [PluginReference]
        private Plugin CopyPaste;

        private const ulong DefaultSkinId = 2982389320;
        private const int DefaultItemID = -180129657; 

        private class BuildingConfig
        {
            public ulong SkinId;
            public int ItemID; 
            public string BuildingName;
        }

        private List<BuildingConfig> buildingConfigs;

        private void Init()
        {
            LoadConfig();
            Subscribe(nameof(OnEntityBuilt));
        }

        protected override void LoadDefaultConfig()
        {
            Config["Buildings"] = new List<object>
            {
                new Dictionary<string, object>
                {
                    {"SkinId", DefaultSkinId},
                    {"ItemID", DefaultItemID},
                    {"BuildingName", "home"},
                }
            };
            SaveConfig();
        }

        private void LoadConfig()
        {
            buildingConfigs = new List<BuildingConfig>();
            var buildingList = Config["Buildings"] as List<object>;

            foreach (var building in buildingList)
            {
                var buildingData = building as Dictionary<string, object>;
                if (buildingData != null)
                {
                    buildingConfigs.Add(new BuildingConfig
                    {
                        SkinId = ulong.Parse(buildingData["SkinId"].ToString()),
                        ItemID = int.Parse(buildingData["ItemID"].ToString()),
                        BuildingName = buildingData["BuildingName"].ToString(),
                    });
                }
            }
        }

        private void OnEntityBuilt(Planner planner, GameObject gameObject)
        {
            if (gameObject.TryGetComponent<BaseEntity>(out BaseEntity entity))
            {
                var player = planner.GetOwnerPlayer();
                if (player == null) return;

                foreach (var config in buildingConfigs)
                {
                    if (entity.skinID == config.SkinId)
                    {
                        entity.Kill();
                        var newItem = ItemManager.CreateByItemID(config.ItemID, 1);
                        newItem.skin = config.SkinId;

                        bool success = TryPasteBuilding(player, config.BuildingName);
                        if (!success)
                        {
                            player.GiveItem(newItem);
                            PrintToChat(player, "Не удалось выполнить вставку здания. Предмет возвращён.");
                        }
                        else
                        {
                            SendReply(player, "Здание успешно вставлено.");
                        }
                        break;
                    }
                }
            }
        }

        private void LogInventory(BasePlayer player)
        {
            foreach (var item in player.inventory.containerMain.itemList)
            {}
        }

        private bool TryPasteBuilding(BasePlayer player, string buildingName)
        {
            if (CopyPaste == null)
            {
                Puts("Плагин CopyPaste не найден.");
                return false;
            }

            Vector3 position = player.transform.position;
            float rotationCorrection = 0f;

            var options = new List<string> { "blockcollision", "0.1", "auth", "true", "autoheight", "true", "entityowner", "true", "height", "1" };

            var success = CopyPaste?.Call("TryPasteFromVector3", position, rotationCorrection, buildingName, options.ToArray());

            if (success is string errorMessage)
            {
                return false;
            }

            return true;
        }
    }
}
