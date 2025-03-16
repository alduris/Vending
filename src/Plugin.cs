global using Random = UnityEngine.Random;
using BepInEx;
using BepInEx.Logging;
using System;
using System.Linq;
using System.Security.Permissions;

// Allows access to private members
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace VendingMod;

[BepInPlugin("alduris.vending", "Vending Machines", "1.0")]
sealed class Plugin : BaseUnityPlugin
{
    public static new ManualLogSource Logger;
    public static bool M4rblelous = false;
    public static bool Shrembly = false;
    bool IsInit;

    public void OnEnable()
    {
        Logger = base.Logger;
        On.RainWorld.PostModsInit += OnModsInit; // after for no good reason
    }

    private void OnModsInit(On.RainWorld.orig_PostModsInit orig, RainWorld self)
    {
        orig(self);
        M4rblelous = ModManager.ActiveMods.Any(x => x.id == "lb-fgf-m4r-ik.modpack");
        Shrembly = ModManager.ActiveMods.Any(x => x.id == "com.rainworldgame.shroudedassembly.plugin");

        if (IsInit) return;
        IsInit = true;

        try
        {
            Enums.Register();

            VendingHooks.Apply();
            PebbsiHooks.Apply();
        }
        catch (Exception e)
        {
            Logger.LogError(e);
        }
    }
}
