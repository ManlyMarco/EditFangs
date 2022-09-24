using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using BepInEx;
using BepInEx.Logging;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker.UI;
using UniRx;
using KKAPI.Maker;

namespace EditFangs
{
    [BepInPlugin(GUID, PluginName, Version)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    public class EditFangsPlugin : BaseUnityPlugin
    {
#if KK
        public const string PluginName = "KK_EditFangs";
#elif KKS
        public const string PluginName = "KKS_EditFangs";
#endif
        public const string GUID = "org.njaecha.plugins.editfangs";
        public const string Version = "1.1.0";

        internal static new ManualLogSource Logger;

        private void Awake()
        {
            Logger = base.Logger;
            CharacterApi.RegisterExtraBehaviour<EditFangsController>(GUID);
            MakerAPI.RegisterCustomSubCategories += RegisterCustomSubCategories;
        }

        private void RegisterCustomSubCategories(object sender, RegisterSubCategoriesEvent e)
        {
            e.AddControl(new MakerSlider(MakerConstants.Face.Mouth, "Left Fang Length", 0f, 1f, 0.1f, this))
                .BindToFunctionController<EditFangsController, float>(controller => controller.fangData.scaleL, (controller, value) => controller.fangData.scaleL = value);
            e.AddControl(new MakerSlider(MakerConstants.Face.Mouth, "Left Fang Spacing", 0f, 1.3f, 1f, this))
                .BindToFunctionController<EditFangsController, float>(controller => controller.fangData.spacingL, (controller, value) => controller.fangData.spacingL = value);
            e.AddControl(new MakerSlider(MakerConstants.Face.Mouth, "Right Fang Length", 0f, 1f, 0.1f, this))
                .BindToFunctionController<EditFangsController, float>(controller => controller.fangData.scaleR, (controller, value) => controller.fangData.scaleR = value);
            e.AddControl(new MakerSlider(MakerConstants.Face.Mouth, "Right Fang Spacing", 0f, 1.3f, 1f, this))
                .BindToFunctionController<EditFangsController, float>(controller => controller.fangData.spacingR, (controller, value) => controller.fangData.spacingR = value);

            Singleton<ChaCustom.CustomBase>.Instance.actUpdateCvsMouth += () => MakerAPI.GetCharacterControl().GetComponent<EditFangsController>().registerFangs();
        }
    }
}
