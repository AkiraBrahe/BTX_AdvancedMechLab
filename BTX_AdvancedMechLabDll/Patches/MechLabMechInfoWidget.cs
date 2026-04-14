using BattleTech;
using BattleTech.UI;
using BattleTech.UI.TMProWrapper;
using BattleTech.UI.Tooltips;
using BTX_AdvancedMechLab.Features.Widgets;
using CustomUnits;
using HBS.Extensions;
using SVGImporter;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BTX_AdvancedMechLab.Patches
{
    /// <summary>
    /// Adds widgets to the mech info panel and adjusts the layout to accommodate them.
    /// </summary>
    [HarmonyPatch(typeof(MechLabMechInfoWidget), "RefreshInfo")]
    public static class MechLabMechInfoWidget_RefreshInfo
    {
        [HarmonyPostfix]
        public static void Postfix(MechLabMechInfoWidget __instance)
        {
            var mechDef = __instance.mechLab.CreateMechDef();
            if (mechDef == null) return;

            bool isVehicle = mechDef.IsVehicle();

            var layoutTonnage = __instance.remainingTonnage.transform.parent;
            var objStatus = layoutTonnage.parent;
            var container = objStatus.Find("custom_capacities");
            var layoutHardpoints = objStatus.Find("layout_hardpoints");

            GetOrSetupWidgets(__instance, objStatus, container, layoutTonnage, layoutHardpoints,
                out var armorWidget, out var coolingWidget);

            container.gameObject.SetActive(!isVehicle);

            var button = layoutHardpoints.Find("OBJ_stockBttn");
            button?.gameObject.SetActive(false);

            if (!isVehicle)
            {
                armorWidget.Refresh(mechDef);
                coolingWidget.Refresh(mechDef);
            }
        }

        private static void GetOrSetupWidgets(
            MechLabMechInfoWidget infoWidget,
            Transform objStatus,
            Transform container,
            Transform layoutTonnage,
            Transform layoutHardpoints,
            out ArmorWidget armorWidget,
            out CoolingWidget coolingWidget)
        {
            var vGroup = objStatus.gameObject.GetComponent<VerticalLayoutGroup>();
            if (vGroup == null)
            {
                SetupVerticalLayout(objStatus, layoutTonnage);
            }

            if (container == null)
            {
                container = SetupCustomContainer(objStatus, layoutHardpoints);
            }

            SetupWidgets(infoWidget, container, out armorWidget, out coolingWidget);
        }

        private static void SetupVerticalLayout(Transform objStatus, Transform layoutTonnage)
        {
            objStatus.GetComponent<RectTransform>().pivot = new Vector2(0.05f, 1);

            var vGroup = objStatus.gameObject.AddComponent<VerticalLayoutGroup>();
            vGroup.childControlWidth = false; vGroup.childControlHeight = true;
            vGroup.childForceExpandWidth = false; vGroup.childForceExpandHeight = false;
            vGroup.spacing = 8f;

            foreach (Transform child in objStatus)
            {
                var layout = child.gameObject.GetOrAddComponent<LayoutElement>();
                var childRect = child.GetComponent<RectTransform>();
                if (layout.preferredHeight <= 0) layout.preferredHeight = childRect.rect.height;
            }

            var tonnageRect = layoutTonnage.GetComponent<RectTransform>();
            tonnageRect.sizeDelta = new Vector2(220, 68);
            tonnageRect.pivot = new Vector2(0, 1);

            var tonnageLayout = layoutTonnage.gameObject.GetOrAddComponent<LayoutElement>();
            tonnageLayout.preferredWidth = 220; tonnageLayout.preferredHeight = 68;
            tonnageLayout.minWidth = 220; tonnageLayout.minHeight = 68;
        }

        private static Transform SetupCustomContainer(Transform objStatus, Transform layoutHardpoints)
        {
            var go = new GameObject("custom_capacities");
            var containerRect = go.AddComponent<RectTransform>();
            containerRect.anchorMin = containerRect.anchorMax = new Vector2(0, 1);
            containerRect.pivot = new Vector2(0, 1);
            containerRect.sizeDelta = new Vector2(0, 72);

            var layout = go.AddComponent<LayoutElement>();
            layout.preferredHeight = 72;

            var container = go.transform;
            container.SetParent(objStatus, false);
            container.SetSiblingIndex(layoutHardpoints.GetSiblingIndex());

            var vGroup = go.AddComponent<VerticalLayoutGroup>();
            vGroup.childControlWidth = vGroup.childControlHeight = false;
            vGroup.childForceExpandWidth = vGroup.childForceExpandHeight = false;
            vGroup.childAlignment = TextAnchor.LowerLeft;

            return container;
        }

        private static void SetupWidgets(MechLabMechInfoWidget infoWidget, Transform container,
            out ArmorWidget armorWidget, out CoolingWidget coolingWidget)
        {
            armorWidget = container.GetComponentInChildren<ArmorWidget>();
            coolingWidget = container.GetComponentInChildren<CoolingWidget>();

            if (armorWidget != null && coolingWidget != null)
                return;

            var armorRow = CreateDisplayElement(infoWidget, container, "armor", "Standard Armor", "uixSvgIcon_action_end");
            armorWidget = armorRow.AddComponent<ArmorWidget>();
            var armorLabel = armorRow.GetComponentInChildren<LocalizableText>();
            armorLabel.enableWordWrapping = true;

            var coolingRow = CreateDisplayElement(infoWidget, container, "heatsink", "10/10", "uixSvgIcon_equipment_Heatsink");
            coolingWidget = coolingRow.AddComponent<CoolingWidget>();
        }

        private static GameObject CreateDisplayElement(MechLabMechInfoWidget infoWidget, Transform parent, string suffix, string defaultText, string iconAsset)
        {
            var originalHP = infoWidget.hardpoints[0].gameObject;
            var element = GameObject.Instantiate(originalHP, parent);
            element.name = $"uixPrfIcon_{suffix}-Element-MANAGED";

            var hpComp = element.GetComponent<MechLabHardpointElement>();
            if (hpComp != null) GameObject.Destroy(hpComp);

            element.AddComponent<Image>();
            var color = element.AddComponent<UIColorRefTracker>();
            color.SetUIColor(UIColor.DarkGray);

            var rect = element.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(130, 36);

            var labelGo = element.transform.Find("txt_hardpointQty").gameObject;
            labelGo.name = "txt_label";
            var label = labelGo.GetComponent<LocalizableText>();
            label.SetText(defaultText);
            label.fontSize = 16;
            label.fontSizeMin = 12;
            label.fontSizeMax = 16;
            label.alignment = TextAlignmentOptions.Right;

            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(90, 36);
            labelRect.anchoredPosition = new Vector2(-40, -2);

            var iconGo = element.transform.Find("icon").gameObject;
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchoredPosition = new Vector2(-8, 0);
            iconRect.sizeDelta = new Vector2(24, 24);

            var iconImage = iconGo.GetComponent<SVGImage>();
            var dm = UnityGameInstance.BattleTechGame.DataManager;
            var asset = dm.SVGCache.GetAsset(iconAsset);

            if (asset == null && dm.ResourceLocator.EntryByID(iconAsset, BattleTechResourceType.SVGAsset) != null)
            {
                var loadRequest = dm.CreateLoadRequest();
                loadRequest.AddLoadRequest<SVGAsset>(BattleTechResourceType.SVGAsset, iconAsset, null);
                loadRequest.ProcessRequests();
                asset = dm.SVGCache.GetAsset(iconAsset);
            }

            if (asset != null)
            {
                iconImage.vectorGraphics = asset;
            }

            var tooltip = element.AddComponent<HBSTooltip>();
            tooltip.defaultStateData.SetObject(null);

            return element;
        }
    }
}