﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Partiality.Modloader;
using UnityEngine;
using OptionalUI;
using System.Reflection;
using System.Reflection.Emit;
using System.IO;
using RWCustom;

public class RainScript : MonoBehaviour
{
    public static Downpour mod;
    public void Initialize()
    {
        RainFall.Patch();
        RainPalette.Patch();
    }
    public void Update()
    {

    }
}
public class Downpour : PartialityMod
{
    public Downpour()
    {
        this.ModID = "Downpour";
        this.Version = "Beta";
        this.author = "LeeMoriya";
    }

    public static RainScript script;
    public static bool paletteChange = true;
    public static bool lightning = true;
    public static bool dynamic = true;
    public static int intensity = 0;
    public static bool rainbow = false;

    public override void OnEnable()
    {
        base.OnEnable();
        RainScript.mod = this;
        GameObject go = new GameObject();
        script = go.AddComponent<RainScript>();
        script.Initialize();
    }
    public OptionalUI.OptionInterface LoadOI()
    {
        if (oiType == null)
            MakeOIType();

        return (OptionalUI.OptionInterface)Activator.CreateInstance(oiType, new object[] { this });
    }

    private Type oiType = null;
    private void MakeOIType()
    {
        Debug.Log("Loading DownpourOptions...");
        AssemblyName name = new AssemblyName("DownpourOI");
        AssemblyBuilder ab = AppDomain.CurrentDomain.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);
        ModuleBuilder mb = ab.DefineDynamicModule(name.Name);
        TypeBuilder tb = mb.DefineType("DownpourOptions", TypeAttributes.Class, typeof(OptionalUI.OptionInterface));

        ConstructorBuilder cb = tb.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[] { typeof(Partiality.Modloader.PartialityMod) });
        ILGenerator ctorILG = cb.GetILGenerator();
        ctorILG.Emit(OpCodes.Ldarg_0);
        ctorILG.Emit(OpCodes.Ldarg_1);
        ctorILG.Emit(OpCodes.Call, typeof(OptionalUI.OptionInterface).GetConstructor(new Type[] { typeof(Partiality.Modloader.PartialityMod) }));
        ctorILG.Emit(OpCodes.Ret);

        MethodBuilder initmb = tb.DefineMethod("Initialize", MethodAttributes.Virtual | MethodAttributes.Public | MethodAttributes.ReuseSlot | MethodAttributes.HideBySig);
        ILGenerator initmbILG = initmb.GetILGenerator();
        initmbILG.Emit(OpCodes.Ldarg_0);
        initmbILG.Emit(OpCodes.Call, typeof(OptionalUI.OptionInterface).GetMethod("Initialize", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
        initmbILG.Emit(OpCodes.Ldarg_0);
        initmbILG.Emit(OpCodes.Call, typeof(DOProxy).GetMethod("Initialize", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static));
        initmbILG.Emit(OpCodes.Ret);

        MethodBuilder ccmb = tb.DefineMethod("ConfigOnChange", MethodAttributes.Virtual | MethodAttributes.Public | MethodAttributes.ReuseSlot | MethodAttributes.HideBySig);
        ILGenerator ccmbILG = ccmb.GetILGenerator();
        ccmbILG.Emit(OpCodes.Ldarg_0);
        ccmbILG.Emit(OpCodes.Call, typeof(OptionalUI.OptionInterface).GetMethod("ConfigOnChange", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
        ccmbILG.Emit(OpCodes.Ldarg_0);
        ccmbILG.Emit(OpCodes.Call, typeof(DOProxy).GetMethod("ConfigOnChange", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static));
        ccmbILG.Emit(OpCodes.Ret);

        oiType = tb.CreateType();
        Debug.Log("Loaded DownpourOptions");
    }
}

public class DOProxy
{
    //Setup ConfigMachine GUI
    public static void Initialize(OptionalUI.OptionInterface self)
    {
        string[] regionList = File.ReadAllLines(Custom.RootFolderDirectory() + "/World/Regions/regions.txt");
        self.Tabs = new OpTab[2];
        self.Tabs[0] = new OpTab("Options");
        self.Tabs[1] = new OpTab("Regions");
        //Rain
        OptionalUI.OpLabel rainIntensity = new OpLabel(new Vector2(30f, 560f), new Vector2(400f, 40f), "Rain Intensity", FLabelAlignment.Left, true);
        self.Tabs[0].AddItem(rainIntensity);
        OptionalUI.OpLabel rainIntensityDescription = new OpLabel(new Vector2(30f, 537f), new Vector2(400f, 40f), "Change the intensity of the rainfall to be dynamic, or a fixed value.", FLabelAlignment.Left, false);
        self.Tabs[0].AddItem(rainIntensityDescription);
        OptionalUI.OpRadioButtonGroup intensityGroup = new OpRadioButtonGroup("Setting", 0);
        OptionalUI.OpRadioButton intensityDynamic = new OpRadioButton(new Vector2(30f, 510f));
        intensityDynamic.description = "Intensity of the rain is randomly determined and affected by karma level, there can also be no rain at all.";
        self.Tabs[0].AddItem(intensityDynamic);
        OptionalUI.OpLabel dynamicLabel = new OpLabel(new Vector2(60f, 503f), new Vector2(400f, 40f), "Dynamic", FLabelAlignment.Left, false);
        self.Tabs[0].AddItem(dynamicLabel);
        OptionalUI.OpRadioButton intensityLow = new OpRadioButton(new Vector2(130f, 510f));
        intensityLow.description = "Intensity of the rain will be fixed to Low intensity.";
        self.Tabs[0].AddItem(intensityLow);
        OptionalUI.OpLabel lowLabel = new OpLabel(new Vector2(160f, 503f), new Vector2(400f, 40f), "Low", FLabelAlignment.Left, false);
        self.Tabs[0].AddItem(lowLabel);
        OptionalUI.OpRadioButton intensityMed = new OpRadioButton(new Vector2(210f, 510f));
        intensityMed.description = "Intensity of the rain will be fixed to Medium intensity.";
        self.Tabs[0].AddItem(intensityMed);
        OptionalUI.OpLabel medLabel = new OpLabel(new Vector2(240f, 503f), new Vector2(400f, 40f), "Med", FLabelAlignment.Left, false);
        self.Tabs[0].AddItem(medLabel);
        OptionalUI.OpRadioButton intensityHigh = new OpRadioButton(new Vector2(290f, 510f));
        intensityHigh.description = "Intensity of the rain will be fixed to High intensity.";
        self.Tabs[0].AddItem(intensityHigh);
        OptionalUI.OpLabel highLabel = new OpLabel(new Vector2(320f, 503f), new Vector2(400f, 40f), "High", FLabelAlignment.Left, false);
        self.Tabs[0].AddItem(highLabel);
        intensityGroup.SetButtons(new OpRadioButton[] { intensityDynamic, intensityLow, intensityMed, intensityHigh });
        self.Tabs[0].AddItem(intensityGroup);
        //Lightning
        OptionalUI.OpLabel environmentOption = new OpLabel(new Vector2(30f, 460f), new Vector2(400f, 40f), "Environment", FLabelAlignment.Left, true);
        self.Tabs[0].AddItem(environmentOption);
        OptionalUI.OpLabel lightningOptionDescription = new OpLabel(new Vector2(30f, 437f), new Vector2(400f, 40f), "Configure which effects are added to the environment during heavy rain.", FLabelAlignment.Left, false);
        self.Tabs[0].AddItem(lightningOptionDescription);
        OptionalUI.OpCheckBox lightningCheck = new OpCheckBox(new Vector2(30f, 410f),"Lightning",true);
        lightningCheck.description = "Lightning will appear in regions when rain intensity is high enough.";
        self.Tabs[0].AddItem(lightningCheck);
        OptionalUI.OpLabel lightningLabel = new OpLabel(new Vector2(60f, 403f), new Vector2(400f, 40f), "Lightning storms", FLabelAlignment.Left, false);
        self.Tabs[0].AddItem(lightningLabel);
        //Palette
        OptionalUI.OpCheckBox paletteCheck = new OpCheckBox(new Vector2(30f, 380f),"Palette",true);
        paletteCheck.description = "The region will become darker with higher rain intensity.";
        self.Tabs[0].AddItem(paletteCheck);
        OptionalUI.OpLabel paletteLabel = new OpLabel(new Vector2(60f, 373f), new Vector2(400f, 40f), "Regions become darker", FLabelAlignment.Left, false);
        self.Tabs[0].AddItem(paletteLabel);
        //Rainbow
        OptionalUI.OpCheckBox rainbowOn = new OpCheckBox(new Vector2(30f, 350f),"Rainbow",false);
        rainbowOn.description = "Raindrop colors will be randomized";
        self.Tabs[0].AddItem(rainbowOn);
        OptionalUI.OpLabel onrainbowLabel = new OpLabel(new Vector2(60f, 343f), new Vector2(400f, 40f), "Taste the rainbow", FLabelAlignment.Left, false);
        self.Tabs[0].AddItem(onrainbowLabel);
        //---End of First Tab---

        //---Regions Tab---
        if(regionList != null)
        {
            OptionalUI.OpRadioButtonGroup[] regionGroup = new OpRadioButtonGroup[regionList.Length];
            OptionalUI.OpRadioButton[] regionOnButtons = new OpRadioButton[regionList.Length];
            OptionalUI.OpRadioButton[] regionOffButtons = new OpRadioButton[regionList.Length];
            OptionalUI.OpLabel[] regionOffLabels = new OpLabel[regionList.Length];
            OptionalUI.OpLabel[] regionOnLabels = new OpLabel[regionList.Length];
            OptionalUI.OpLabel[] regionLabelList = new OpLabel[regionList.Length];
            OptionalUI.OpLabel regionLabel = new OpLabel(new Vector2(30f, 560f), new Vector2(400f, 40f), "Region Settings", FLabelAlignment.Left, true);
            OptionalUI.OpLabel regionDescription  = new OpLabel(new Vector2(30f, 535f), new Vector2(400f, 40f), "Enable and Disable rainfall on a per-region basis.", FLabelAlignment.Left, false);
            self.Tabs[1].AddItem(regionLabel);
            self.Tabs[1].AddItem(regionDescription);

            for (int i = 0; i < regionList.Length; i++)
            {
                regionGroup[i] = new OpRadioButtonGroup(regionList[i], 1);
                if (100f * i < 700f)
                {
                    regionLabelList[i] = new OpLabel(new Vector2(30f, 490f - (75f * i)), new Vector2(400f, 40f), regionList[i], FLabelAlignment.Left, true);
                    regionOffButtons[i] = new OpRadioButton(new Vector2(30f, 470f - (75f * i)));
                    regionOnButtons[i] = new OpRadioButton(new Vector2(100f, 470f - (75f * i)));
                    regionOffLabels[i] = new OpLabel(new Vector2(60f, 460f - (75f * i)), new Vector2(400f, 40f), "Off", FLabelAlignment.Left, false);
                    regionOnLabels[i] = new OpLabel(new Vector2(130f, 460f - (75f * i)), new Vector2(400f, 40f), "On", FLabelAlignment.Left, false);
                }
                else
                {
                    regionLabelList[i] = new OpLabel(new Vector2(250f, 1015f - (75f * i)), new Vector2(400f, 40f), regionList[i], FLabelAlignment.Left, true);
                    regionOffButtons[i] = new OpRadioButton(new Vector2(250f, 995f - (75f * i)));
                    regionOnButtons[i] = new OpRadioButton(new Vector2(320f, 995f - (75f * i)));
                    regionOffLabels[i] = new OpLabel(new Vector2(280f, 985f - (75f * i)), new Vector2(400f, 40f), "Off", FLabelAlignment.Left, false);
                    regionOnLabels[i] = new OpLabel(new Vector2(350f, 985f - (75f * i)), new Vector2(400f, 40f), "On", FLabelAlignment.Left, false);
                }
                regionGroup[i].SetButtons(new OpRadioButton[] { regionOffButtons[i], regionOnButtons[i] });
                self.Tabs[1].AddItem(regionLabelList[i]);
                self.Tabs[1].AddItem(regionOffButtons[i]);
                self.Tabs[1].AddItem(regionOnButtons[i]);
                self.Tabs[1].AddItem(regionOffLabels[i]);
                self.Tabs[1].AddItem(regionOnLabels[i]);
                self.Tabs[1].AddItem(regionGroup[i]);
            }
        }
    }

    // Apply changes to the mod
    public static void ConfigOnChange(OptionalUI.OptionInterface self)
    {
        if (OptionalUI.OptionInterface.config["Palette"] == "0")
        {
            Downpour.paletteChange = false;
        }
        else
        {
            Downpour.paletteChange = true;
        }
        if (OptionalUI.OptionInterface.config["Lightning"] == "0")
        {
            Downpour.lightning = false;
        }
        else
        {
            Downpour.lightning = true;
        }
        if (OptionalUI.OptionInterface.config["Rainbow"] == "0")
        {
            Downpour.rainbow = false;
        }
        else
        {
            Downpour.rainbow = true;
        }
        if (OptionalUI.OptionInterface.config["Setting"] == "0")
        {
            Downpour.intensity = 0;
            Downpour.dynamic = true;
        }
        if (OptionalUI.OptionInterface.config["Setting"] == "1")
        {
            Downpour.intensity = 1;
            Downpour.dynamic = false;
        }
        if (OptionalUI.OptionInterface.config["Setting"] == "2")
        {
            Downpour.intensity = 2;
            Downpour.dynamic = false;
        }
        if (OptionalUI.OptionInterface.config["Setting"] == "3")
        {
            Downpour.intensity = 3;
            Downpour.dynamic = false;
        }
    }
}